namespace MegaDescontao.Api.Marketplaces.Providers;

/// Provider de uma lista já pronta de ofertas — usado quando o dado chega por upload em vez
/// de por chamada a uma API. Deixa o caminho do CSV reaproveitar a mesma esteira de
/// importação: upsert por (loja, id externo), histórico de preço e tudo o mais.
public class InMemoryOfferProvider(
    string storeSlug,
    string displayName,
    IReadOnlyList<MarketplaceOffer> offers) : IMarketplaceProvider
{
    public string StoreSlug => storeSlug;

    public string DisplayName => displayName;

    public bool IsConfigured => true;

    /// Um lote enviado à mão é um recorte, nunca o catálogo inteiro da loja: oferta ausente
    /// do arquivo não significa que saiu do ar.
    public bool ProvidesFullSnapshot => false;

    public Task<IReadOnlyList<MarketplaceOffer>> FetchOffersAsync(CancellationToken cancellationToken) =>
        Task.FromResult(offers);
}
