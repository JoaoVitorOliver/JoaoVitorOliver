using MegaDescontao.Api.Models;

namespace MegaDescontao.Api.Contracts;

public record OfferInput(
    string StoreSlug,
    string AffiliateUrl,
    string? ProductUrl,
    string? ExternalProductId,
    decimal CurrentPrice,
    decimal? OriginalPrice,
    OfferStatus Status = OfferStatus.Active,
    DateTime? ExpiresAt = null);

public record ProductInput(
    string Title,
    string? Description,
    string ImageUrl,
    string CategorySlug,
    bool IsActive = true,
    IReadOnlyList<OfferInput>? Offers = null);

/// Visão administrativa: aqui a AffiliateUrl aparece, porque quem chama já é o dono do catálogo.
public record AdminOfferResponse(
    int Id,
    string StoreSlug,
    string StoreName,
    string AffiliateUrl,
    string? ProductUrl,
    string? ExternalProductId,
    decimal CurrentPrice,
    decimal? OriginalPrice,
    int DiscountPercentage,
    OfferStatus Status,
    DateTime? ExpiresAt,
    DateTime? LastCheckedAt,
    int ClickCount,
    DateTime UpdatedAt);

public record AdminProductResponse(
    int Id,
    string Title,
    string? Description,
    string ImageUrl,
    string CategorySlug,
    bool IsActive,
    IReadOnlyList<AdminOfferResponse> Offers,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// Completa o que o CSV de afiliado não traz: a foto e o preço de antes.
/// O OfferId é opcional — com uma oferta só no produto, o preço vai nela.
public record EnrichItem(
    int ProductId,
    string? ImageUrl,
    decimal? OriginalPrice,
    int? OfferId = null);

public record EnrichResultResponse(
    int Received,
    int ImagesUpdated,
    int PricesUpdated,
    IReadOnlyList<string> Warnings);

public record ClickReportItem(
    int OfferId,
    int ProductId,
    string ProductTitle,
    string StoreName,
    int Clicks);

public record ImportResultResponse(
    string Provider,
    string StoreSlug,
    int Received,
    int Created,
    int Updated,
    int PriceChanges,
    int MarkedUnavailable,
    IReadOnlyList<string> Warnings);
