namespace MegaDescontao.Api.Contracts;

public record CategorySummary(string Name, string Slug);

public record StoreSummary(string Name, string Slug, string? LogoUrl);

/// A oferta como o site a mostra. A AffiliateUrl fica de fora de propósito: o acesso
/// à loja passa sempre por /api/go/{offerId}, que é onde o clique é contabilizado.
public record OfferSummary(
    int OfferId,
    StoreSummary Store,
    decimal CurrentPrice,
    decimal? OriginalPrice,
    int DiscountPercentage,
    int ClickCount,
    DateTime? ExpiresAt);

public record ProductListItem(
    int Id,
    string Title,
    string? Description,
    string ImageUrl,
    CategorySummary Category,
    OfferSummary BestOffer,
    int OfferCount,
    DateTime CreatedAt);

public record ProductDetail(
    int Id,
    string Title,
    string? Description,
    string ImageUrl,
    CategorySummary Category,
    IReadOnlyList<OfferSummary> Offers,
    decimal? LowestPrice30Days,
    decimal? AveragePrice30Days,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int Total,
    int TotalPages);
