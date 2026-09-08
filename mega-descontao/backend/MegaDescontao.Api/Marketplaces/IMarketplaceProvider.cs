namespace MegaDescontao.Api.Marketplaces;

/// Oferta já traduzida para o vocabulário do Mega Descontão. Cada marketplace responde
/// no formato dele (title/price/image vs name/current_price/image_url); a tradução acontece
/// dentro do provider, e daqui para dentro o sistema só conhece este formato.
public record MarketplaceOffer(
    string ExternalProductId,
    string Title,
    string? Description,
    string ImageUrl,
    decimal CurrentPrice,
    decimal? OriginalPrice,
    string ProductUrl,
    string AffiliateUrl,
    string CategoryName,
    DateTime? ExpiresAt = null);

public interface IMarketplaceProvider
{
    /// Slug da loja em Stores. É a chave que liga o provider ao catálogo.
    string StoreSlug { get; }

    string DisplayName { get; }

    /// Falso enquanto faltarem credenciais ou aprovação do programa de afiliados.
    /// O importador não tenta chamar um provider não configurado.
    bool IsConfigured { get; }

    /// Verdadeiro se a resposta representa o catálogo inteiro da loja. Só nesse caso o
    /// importador pode concluir que uma oferta ausente saiu do ar; num feed parcial
    /// (ex.: só "ofertas do dia") a ausência não significa nada.
    bool ProvidesFullSnapshot { get; }

    Task<IReadOnlyList<MarketplaceOffer>> FetchOffersAsync(CancellationToken cancellationToken);
}
