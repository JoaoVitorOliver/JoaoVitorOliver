using System.Text.Json;
using MegaDescontao.Api.Marketplaces.Providers.Shopee;

namespace MegaDescontao.Tests;

public class ShopeeOfferMapperTests
{
    private static readonly ShopeeOptions Options = new()
    {
        DefaultCategory = "Ofertas Shopee",
        CategoryMap = new Dictionary<string, string> { ["100017"] = "Eletrônicos" },
    };

    private static ShopeeOfferNode NodeValido() => new()
    {
        ItemId = 987654321,
        ShopId = 12345,
        ProductName = "  Smartwatch com Monitor Cardíaco  ",
        ImageUrl = "https://cf.shopee.com.br/file/abc",
        PriceMin = 149.00m,
        PriceMax = 189.00m,
        PriceDiscountRate = 55m,
        OfferLink = "https://s.shopee.com.br/abc123",
        ProductLink = "https://shopee.com.br/product/12345/987654321",
        ProductCatIds = [100017],
        PeriodEndTime = 1789000000,
    };

    [Fact]
    public void Traduz_a_oferta_para_o_formato_interno()
    {
        var offer = ShopeeOfferMapper.ToMarketplaceOffer(NodeValido(), Options);

        Assert.NotNull(offer);
        Assert.Equal("12345-987654321", offer.ExternalProductId);
        Assert.Equal("Smartwatch com Monitor Cardíaco", offer.Title);
        Assert.Equal(149.00m, offer.CurrentPrice);
        Assert.Equal(331.11m, offer.OriginalPrice);
        Assert.Equal("https://s.shopee.com.br/abc123", offer.AffiliateUrl);
        Assert.Equal("https://shopee.com.br/product/12345/987654321", offer.ProductUrl);
        Assert.Equal("Eletrônicos", offer.CategoryName);
        Assert.Equal(new DateTime(2026, 9, 10, 0, 26, 40, DateTimeKind.Utc), offer.ExpiresAt);
        Assert.Null(offer.Description);
    }

    [Fact]
    public void Usa_o_menor_preco_quando_o_produto_tem_faixa_de_variacao()
    {
        var offer = ShopeeOfferMapper.ToMarketplaceOffer(NodeValido(), Options);

        Assert.Equal(149.00m, offer!.CurrentPrice);
    }

    [Fact]
    public void Reconstroi_o_preco_original_a_partir_do_percentual()
    {
        Assert.Equal(331.11m, ShopeeOfferMapper.DeriveOriginalPrice(149.00m, 55m));
        Assert.Equal(128.43m, ShopeeOfferMapper.DeriveOriginalPrice(89.90m, 30m));
    }

    [Fact]
    public void Sem_desconto_valido_nao_inventa_preco_original()
    {
        Assert.Null(ShopeeOfferMapper.DeriveOriginalPrice(149.00m, null));
        Assert.Null(ShopeeOfferMapper.DeriveOriginalPrice(149.00m, 0m));
        Assert.Null(ShopeeOfferMapper.DeriveOriginalPrice(149.00m, 100m));
        Assert.Null(ShopeeOfferMapper.DeriveOriginalPrice(149.00m, -5m));
    }

    [Fact]
    public void Oferta_sem_link_de_afiliado_e_descartada()
    {
        var node = NodeValido();
        node.OfferLink = null;

        Assert.Null(ShopeeOfferMapper.ToMarketplaceOffer(node, Options));
    }

    [Fact]
    public void Oferta_sem_nome_e_descartada()
    {
        var node = NodeValido();
        node.ProductName = "   ";

        Assert.Null(ShopeeOfferMapper.ToMarketplaceOffer(node, Options));
    }

    [Fact]
    public void Oferta_sem_preco_utilizavel_e_descartada()
    {
        var node = NodeValido();
        node.PriceMin = 0m;
        node.PriceMax = null;

        Assert.Null(ShopeeOfferMapper.ToMarketplaceOffer(node, Options));
    }

    [Fact]
    public void Categoria_desconhecida_cai_no_padrao()
    {
        var node = NodeValido();
        node.ProductCatIds = [999999];

        var offer = ShopeeOfferMapper.ToMarketplaceOffer(node, Options);

        Assert.Equal("Ofertas Shopee", offer!.CategoryName);
    }

    [Fact]
    public void Sem_productLink_o_link_de_afiliado_vira_a_url_do_produto()
    {
        var node = NodeValido();
        node.ProductLink = null;

        var offer = ShopeeOfferMapper.ToMarketplaceOffer(node, Options);

        Assert.Equal(offer!.AffiliateUrl, offer.ProductUrl);
    }

    [Fact]
    public void Promocao_sem_prazo_fica_sem_data_de_expiracao()
    {
        var node = NodeValido();
        node.PeriodEndTime = 0;

        var offer = ShopeeOfferMapper.ToMarketplaceOffer(node, Options);

        Assert.Null(offer!.ExpiresAt);
    }

    [Fact]
    public void Le_a_resposta_com_numeros_como_string_e_como_numero()
    {
        // A Shopee devolve preço como string e ids ora como número, ora como string.
        // A leitura tem que aguentar as duas formas sem estourar.
        const string json = """
            {
              "data": {
                "productOfferV2": {
                  "nodes": [
                    {
                      "itemId": "987654321",
                      "shopId": 12345,
                      "productName": "Produto A",
                      "imageUrl": "https://cf.shopee.com.br/file/a",
                      "priceMin": "149.00",
                      "priceMax": 189.00,
                      "priceDiscountRate": "55",
                      "offerLink": "https://s.shopee.com.br/a",
                      "productCatIds": [100017],
                      "periodEndTime": "1789000000"
                    }
                  ],
                  "pageInfo": { "page": 1, "limit": 50, "hasNextPage": true }
                }
              }
            }
            """;

        var parsed = JsonSerializer.Deserialize<ShopeeGraphQlResponse>(json, ShopeeJson.Options);
        var node = parsed!.Data!.ProductOfferV2!.Nodes!.Single();

        Assert.Equal(987654321, node.ItemId);
        Assert.Equal(12345, node.ShopId);
        Assert.Equal(149.00m, node.PriceMin);
        Assert.Equal(189.00m, node.PriceMax);
        Assert.Equal(55m, node.PriceDiscountRate);
        Assert.Equal(1789000000, node.PeriodEndTime);
        Assert.True(parsed.Data.ProductOfferV2.PageInfo!.HasNextPage);

        var offer = ShopeeOfferMapper.ToMarketplaceOffer(node, Options);
        Assert.Equal("12345-987654321", offer!.ExternalProductId);
        Assert.Equal(331.11m, offer.OriginalPrice);
    }

    [Fact]
    public void Erro_de_graphql_e_reconhecido_na_resposta()
    {
        const string json = """
            { "errors": [ { "message": "10035 no api permission" } ] }
            """;

        var parsed = JsonSerializer.Deserialize<ShopeeGraphQlResponse>(json, ShopeeJson.Options);

        Assert.NotNull(parsed!.Errors);
        Assert.Contains("10035", parsed.Errors!.Single().Message);
    }
}
