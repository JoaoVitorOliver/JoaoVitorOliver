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
