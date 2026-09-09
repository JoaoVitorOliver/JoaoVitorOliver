namespace MegaDescontao.Api.Marketplaces.Providers;

public class MarketplaceCredentials
{
    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    /// Identificador do afiliado usado para montar o link rastreado.
    public string? AffiliateId { get; set; }
}

/// Integração com a API oficial do Mercado Livre.
///
/// Ainda não implementada de propósito: a busca de itens exige aplicação registrada e
/// token OAuth, e o link que gera comissão precisa sair do Programa de Afiliados — dados
/// raspados da página não são atribuídos a ninguém. Enquanto as credenciais não existem,
/// o provider se declara não configurado e o importador o ignora sem quebrar nada.
public class MercadoLivreProvider(MarketplaceCredentials credentials) : IMarketplaceProvider
{
    public string StoreSlug => "mercado-livre";

    public string DisplayName => "Mercado Livre";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(credentials.ClientId) &&
        !string.IsNullOrWhiteSpace(credentials.ClientSecret);

    public bool ProvidesFullSnapshot => false;

    public Task<IReadOnlyList<MarketplaceOffer>> FetchOffersAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException(
            "Integração com a API do Mercado Livre pendente: registrar a aplicação, obter o token OAuth " +
            "e montar o link pelo Programa de Afiliados antes de habilitar.");
}

