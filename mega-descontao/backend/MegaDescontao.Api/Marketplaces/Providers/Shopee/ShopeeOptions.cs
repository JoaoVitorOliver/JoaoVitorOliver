namespace MegaDescontao.Api.Marketplaces.Providers.Shopee;

public class ShopeeOptions : MarketplaceCredentials
{
    public string Endpoint { get; set; } = "https://open-api.affiliate.shopee.com.br/graphql";

    /// Quantas ofertas trazer por importação.
    public int MaxOffers { get; set; } = 50;

    public int PageSize { get; set; } = 50;

    /// A Shopee não publica o limite de requisições da API de afiliado. Até medirmos o
    /// limite real, uma pausa entre páginas é mais barata que tomar bloqueio.
    public int DelayBetweenPagesMs { get; set; } = 1100;

    /// A API devolve categoria como id numérico. Preencha o de-para conforme os ids
    /// aparecerem nas respostas; o que não estiver aqui cai na categoria padrão.
    public Dictionary<string, string> CategoryMap { get; set; } = [];

    public string DefaultCategory { get; set; } = "Ofertas Shopee";
}
