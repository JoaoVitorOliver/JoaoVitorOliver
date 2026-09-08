namespace MegaDescontao.Api.Models;

public class Offer
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal? OriginalPrice { get; set; }

    /// Nome do marketplace (Mercado Livre, Shopee, Temu...).
    public string Store { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    /// Destino final do redirect de /api/go/{id}. Nunca é exposto na API pública.
    public string AffiliateUrl { get; set; } = string.Empty;

    public int ClickCount { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<OfferClick> Clicks { get; set; } = [];

    public int DiscountPercentage => OriginalPrice > 0 && OriginalPrice > Price
        ? (int)Math.Round((1 - Price / OriginalPrice.Value) * 100)
        : 0;
}
