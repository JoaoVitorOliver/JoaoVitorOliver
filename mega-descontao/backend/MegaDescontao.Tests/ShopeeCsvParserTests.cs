using MegaDescontao.Api.Marketplaces.Providers.Shopee;

namespace MegaDescontao.Tests;

public class ShopeeCsvParserTests
{
    /// Amostra real exportada da Plataforma de Afiliados da Shopee, incluindo as
    /// esquisitices do arquivo: preço com vírgula entre aspas, "1mil+" na coluna de vendas,
    /// nome de loja com ponto e linha em branco no fim.
    private const string CsvReal = """
        Item Id,Item Name,Price,Sales,Nome da loja,Commission Rate,Commission,Product Link,Offer Link
        58264771063,Body Splash Morango e Leite Desodorante Colônia,"14,99",48,Belle.Charm.Ofc,14%,"R$2,10",https://shopee.com.br/product/1867024047/58264771063,https://s.shopee.com.br/6AksKPoKHl
        58254626495,Video Game Retrô SF900 Super Nintendo 2400 jogos + 2 controles s/ fio Otimo Qualidade,"148,98",976,leemodas,17%,"R$25,33",https://shopee.com.br/product/404900314/58254626495,https://s.shopee.com.br/6L4IWingwo
        58210250035,Fone de ouvido Bluetooth Sem Fio Headphone Bluetooth Recarregável P9 Air Top Casual Esportivo,"24,98",1mil+,soooi,10%,"R$2,50",https://shopee.com.br/product/1249391293/58210250035,https://s.shopee.com.br/40gNkQwZgO

        """;

    [Fact]
    public void Le_todas_as_linhas_uteis_e_ignora_a_linha_vazia_do_fim()
    {
        var offers = ShopeeCsvParser.Parse(CsvReal, "Ofertas Shopee");

        Assert.Equal(3, offers.Count);
    }

    [Fact]
    public void Traduz_a_primeira_linha_corretamente()
    {
        var offer = ShopeeCsvParser.Parse(CsvReal, "Beleza")[0];

        Assert.Equal("1867024047-58264771063", offer.ExternalProductId);
        Assert.Equal("Body Splash Morango e Leite Desodorante Colônia", offer.Title);
        Assert.Equal(14.99m, offer.CurrentPrice);
        Assert.Equal("https://s.shopee.com.br/6AksKPoKHl", offer.AffiliateUrl);
        Assert.Equal("https://shopee.com.br/product/1867024047/58264771063", offer.ProductUrl);
        Assert.Equal("Beleza", offer.CategoryName);
    }

    [Fact]
    public void Nao_inventa_o_que_o_csv_nao_traz()
    {
        var offer = ShopeeCsvParser.Parse(CsvReal, "Ofertas Shopee")[0];

        Assert.Null(offer.OriginalPrice);
        Assert.Null(offer.Description);
        Assert.Null(offer.ExpiresAt);
        Assert.Equal(string.Empty, offer.ImageUrl);
    }

    [Fact]
    public void Preco_entre_aspas_com_virgula_nao_quebra_as_colunas()
    {
        // "148,98" tem vírgula dentro do campo. Um split ingênuo por vírgula deslocaria
        // todas as colunas seguintes e o link de afiliado viria errado.
        var offer = ShopeeCsvParser.Parse(CsvReal, "Games")[1];

        Assert.Equal(148.98m, offer.CurrentPrice);
        Assert.Equal("https://s.shopee.com.br/6L4IWingwo", offer.AffiliateUrl);
    }

    [Fact]
    public void Coluna_de_vendas_nao_numerica_nao_atrapalha()
    {
        // A coluna Sales traz "1mil+" em vez de número. Como não a usamos, a linha
        // tem que passar inteira mesmo assim.
        var offer = ShopeeCsvParser.Parse(CsvReal, "Eletrônicos")[2];

        Assert.Equal(24.98m, offer.CurrentPrice);
        Assert.Equal("1249391293-58210250035", offer.ExternalProductId);
    }

    [Fact]
    public void Id_externo_sai_no_mesmo_formato_do_provider_da_api()
    {
        // shopId-itemId é o que o ShopeeProvider produz. Ter o mesmo formato faz a Open API,
        // quando liberada, atualizar estas ofertas em vez de criar duplicatas.
        var id = ShopeeCsvParser.BuildExternalId("https://shopee.com.br/product/1867024047/58264771063", "58264771063");

        Assert.Equal("1867024047-58264771063", id);
    }

    [Fact]
    public void Sem_link_de_produto_cai_para_o_id_do_item()
    {
        Assert.Equal("58264771063", ShopeeCsvParser.BuildExternalId(null, "58264771063"));
        Assert.Equal("58264771063", ShopeeCsvParser.BuildExternalId("https://s.shopee.com.br/abc", " 58264771063 "));
    }

    [Fact]
    public void Le_preco_no_formato_brasileiro()
    {
        Assert.Equal(14.99m, ShopeeCsvParser.ParsePrice("14,99"));
        Assert.Equal(2.10m, ShopeeCsvParser.ParsePrice("R$2,10"));
        Assert.Equal(1148.98m, ShopeeCsvParser.ParsePrice("1.148,98"));
        Assert.Equal(1148.98m, ShopeeCsvParser.ParsePrice(" R$ 1.148,98 "));
    }

    [Fact]
    public void Preco_invalido_ou_vazio_vira_nulo()
    {
        Assert.Null(ShopeeCsvParser.ParsePrice(null));
        Assert.Null(ShopeeCsvParser.ParsePrice("   "));
        Assert.Null(ShopeeCsvParser.ParsePrice("grátis"));
    }

    [Fact]
    public void Linha_sem_link_de_afiliado_e_descartada()
    {
        const string csv = """
            Item Id,Item Name,Price,Product Link,Offer Link
            123,Produto sem link,"10,00",https://shopee.com.br/product/9/123,
            456,Produto com link,"20,00",https://shopee.com.br/product/9/456,https://s.shopee.com.br/ok
            """;

        var offers = ShopeeCsvParser.Parse(csv, "Ofertas Shopee");

        Assert.Single(offers);
        Assert.Equal("Produto com link", offers[0].Title);
    }

    [Fact]
    public void Arquivo_so_com_cabecalho_devolve_lista_vazia()
    {
        const string csv = "Item Id,Item Name,Price,Product Link,Offer Link";

        Assert.Empty(ShopeeCsvParser.Parse(csv, "Ofertas Shopee"));
    }

    [Fact]
    public void Cabecalho_com_BOM_ainda_e_reconhecido()
    {
        var csv = "﻿Item Id,Item Name,Price,Product Link,Offer Link\n" +
                  "123,Produto,\"10,00\",https://shopee.com.br/product/9/123,https://s.shopee.com.br/ok";

        var offers = ShopeeCsvParser.Parse(csv, "Ofertas Shopee");

        Assert.Single(offers);
        Assert.Equal(10.00m, offers[0].CurrentPrice);
    }
}
