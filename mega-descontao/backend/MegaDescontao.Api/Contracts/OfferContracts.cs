namespace MegaDescontao.Api.Contracts;

/// Oferta exposta publicamente. A AffiliateUrl fica de fora de propósito:
/// o acesso à loja passa sempre por /api/go/{id} para que o clique seja contabilizado.
public record OfferResponse(
    int Id,
    string Title,
    string? Description,
    string ImageUrl,
    decimal Price,
    decimal? OriginalPrice,
    int DiscountPercentage,
    string Store,
    string Category,
    int ClickCount,
    bool IsActive,
    DateTime CreatedAt);

public record OfferRequest(
    string Title,
    string? Description,
    string ImageUrl,
    decimal Price,
    decimal? OriginalPrice,
    string Store,
    string Category,
    string AffiliateUrl,
    bool IsActive = true);

public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int Total,
    int TotalPages);

public record FiltersResponse(
    IReadOnlyList<string> Stores,
    IReadOnlyList<string> Categories);
