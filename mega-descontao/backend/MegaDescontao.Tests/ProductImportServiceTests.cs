using MegaDescontao.Api.Data;
using MegaDescontao.Api.Marketplaces;
using MegaDescontao.Api.Marketplaces.Providers;
using MegaDescontao.Api.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MegaDescontao.Tests;

public class ProductImportServiceTests
{
    private static AppDbContext NovoBanco()
    {
        // SQLite em memória: mesmo provider da produção, sem arquivo em disco.
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();

        return db;
    }

    private static (AppDbContext Db, int ProductId) BancoComOfertaCurada()
    {
        var db = NovoBanco();

        var categoria = new Category { Name = "Beleza", Slug = "beleza" };
        var loja = new Store { Name = "Shopee", Slug = "shopee" };

        var produto = new Product
        {
            Title = "Body Splash",
            Category = categoria,
            // Preenchidos à mão na tela de curadoria:
            ImageUrl = "https://cdn.exemplo/foto-curada.jpg",
            SearchText = "body splash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        produto.Offers.Add(new ProductOffer
        {
            Store = loja,
            ExternalProductId = "111-222",
            AffiliateUrl = "https://s.shopee.com.br/antigo",
            CurrentPrice = 14.99m,
            OriginalPrice = 39.90m,
            Status = OfferStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        db.Products.Add(produto);
        db.SaveChanges();

        return (db, produto.Id);
    }

    private static MarketplaceOffer OfertaDoCsv(decimal preco) => new(
        ExternalProductId: "111-222",
        Title: "Body Splash",
        Description: null,
        // O CSV de afiliado da Shopee não traz nem foto nem preço de antes.
        ImageUrl: string.Empty,
        CurrentPrice: preco,
        OriginalPrice: null,
        ProductUrl: "https://shopee.com.br/product/111/222",
        AffiliateUrl: "https://s.shopee.com.br/novo",
        CategoryName: "Beleza");

    private static ProductImportService Servico(AppDbContext db) =>
        new(db, NullLogger<ProductImportService>.Instance);

    [Fact]
    public async Task Importacao_nao_apaga_a_foto_e_o_preco_de_antes_preenchidos_a_mao()
    {
        var (db, produtoId) = BancoComOfertaCurada();
        var provider = new InMemoryOfferProvider("shopee", "Shopee", [OfertaDoCsv(9.99m)]);

        await Servico(db).ImportAsync(provider);

        var produto = await db.Products.Include(p => p.Offers).FirstAsync(p => p.Id == produtoId);
        var oferta = produto.Offers.Single();

        // O que a origem traz, ela atualiza:
        Assert.Equal(9.99m, oferta.CurrentPrice);
        Assert.Equal("https://s.shopee.com.br/novo", oferta.AffiliateUrl);

        // O que a origem NÃO traz continua de pé. Sem esta garantia, a primeira
        // reimportação varreria toda a curadoria.
        Assert.Equal("https://cdn.exemplo/foto-curada.jpg", produto.ImageUrl);
        Assert.Equal(39.90m, oferta.OriginalPrice);
    }

    [Fact]
    public async Task Quando_a_origem_traz_o_dado_ela_manda()
    {
        var (db, produtoId) = BancoComOfertaCurada();

        var daApi = OfertaDoCsv(9.99m) with
        {
            ImageUrl = "https://cf.shopee.com.br/file/oficial",
            OriginalPrice = 29.90m,
            Description = "Descrição vinda da API",
        };

        await Servico(db).ImportAsync(new InMemoryOfferProvider("shopee", "Shopee", [daApi]));

        var produto = await db.Products.Include(p => p.Offers).FirstAsync(p => p.Id == produtoId);

        Assert.Equal("https://cf.shopee.com.br/file/oficial", produto.ImageUrl);
        Assert.Equal("Descrição vinda da API", produto.Description);
        Assert.Equal(29.90m, produto.Offers.Single().OriginalPrice);
    }

    [Fact]
    public async Task Mudanca_de_preco_vira_historico()
    {
        var (db, _) = BancoComOfertaCurada();
        var servico = Servico(db);

        await servico.ImportAsync(new InMemoryOfferProvider("shopee", "Shopee", [OfertaDoCsv(9.99m)]));
        await servico.ImportAsync(new InMemoryOfferProvider("shopee", "Shopee", [OfertaDoCsv(9.99m)]));

        // Duas importações, um preço novo: uma linha de histórico só.
        Assert.Equal(1, await db.PriceHistory.CountAsync());

        await servico.ImportAsync(new InMemoryOfferProvider("shopee", "Shopee", [OfertaDoCsv(7.49m)]));

        Assert.Equal(2, await db.PriceHistory.CountAsync());
    }

    [Fact]
    public async Task Reimportar_o_mesmo_feed_atualiza_em_vez_de_duplicar()
    {
        var (db, _) = BancoComOfertaCurada();
        var servico = Servico(db);

        var primeira = await servico.ImportAsync(new InMemoryOfferProvider("shopee", "Shopee", [OfertaDoCsv(9.99m)]));
        var segunda = await servico.ImportAsync(new InMemoryOfferProvider("shopee", "Shopee", [OfertaDoCsv(9.99m)]));

        Assert.Equal(0, primeira.Created);
        Assert.Equal(1, primeira.Updated);
        Assert.Equal(0, segunda.Created);
        Assert.Equal(1, await db.Offers.CountAsync());
    }

    [Fact]
    public async Task Oferta_vencida_e_marcada_como_expirada()
    {
        var (db, _) = BancoComOfertaCurada();

        var oferta = await db.Offers.FirstAsync();
        oferta.ExpiresAt = DateTime.UtcNow.AddMinutes(-10);
        await db.SaveChangesAsync();

        var expiradas = await Servico(db).ExpireOutdatedOffersAsync();

        Assert.Equal(1, expiradas);

        // AsNoTracking obrigatório: a expiração usa ExecuteUpdateAsync, que grava direto no
        // banco sem passar pelo change tracker. Reler pelo caminho normal devolveria a
        // entidade ainda em cache, com o status antigo.
        Assert.Equal(OfferStatus.Expired, (await db.Offers.AsNoTracking().FirstAsync()).Status);
    }
}
