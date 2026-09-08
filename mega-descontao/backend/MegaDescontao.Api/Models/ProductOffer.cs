namespace MegaDescontao.Api.Models;

public enum OfferStatus
{
    /// Promoção no ar e válida.
    Active = 0,

    /// A promoção acabou (prazo venceu ou o preço voltou ao normal).
    Expired = 1,

    /// O produto saiu do ar ou ficou sem estoque na loja.
    Unavailable = 2,

    /// Escondida manualmente pelo admin.
    Hidden = 3,
}

/// A oferta de um produto em uma loja específica: preço, link de afiliado e situação.
public class ProductOffer
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public int StoreId { get; set; }

    public Store? Store { get; set; }

    /// Id do item na loja de origem (ex.: MLB1234567890). Junto com StoreId forma a
    /// chave natural que torna a importação idempotente.
    public string? ExternalProductId { get; set; }

    /// Página pública do produto na loja, sem parâmetros de afiliado.
    public string? ProductUrl { get; set; }

    /// Destino do /api/go/{id}. Nunca sai do back-end.
    public string AffiliateUrl { get; set; } = string.Empty;

    public decimal CurrentPrice { get; set; }

    public decimal? OriginalPrice { get; set; }

    public OfferStatus Status { get; set; } = OfferStatus.Active;

    /// Quando a promoção deixa de valer, se a loja informar. Ofertas vencidas somem
    /// da vitrine mesmo que ninguém tenha rodado a rotina que marca Expired.
    public DateTime? ExpiresAt { get; set; }

    /// Última vez que o importador confirmou preço e disponibilidade na origem.
    public DateTime? LastCheckedAt { get; set; }

    public int ClickCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<PriceHistory> PriceHistory { get; set; } = [];

    public List<OfferClick> Clicks { get; set; } = [];

    public int DiscountPercentage => OriginalPrice > 0 && OriginalPrice > CurrentPrice
        ? (int)Math.Round((1 - CurrentPrice / OriginalPrice.Value) * 100)
        : 0;
}

/// Um preço observado em um instante. É a base para "menor preço em 30 dias" e para
/// julgar se a promoção é boa de verdade, sem depender do "de/por" informado pela loja.
public class PriceHistory
{
    public int Id { get; set; }

    public int ProductOfferId { get; set; }

    public ProductOffer? ProductOffer { get; set; }

    public decimal Price { get; set; }

    public DateTime CollectedAt { get; set; }
}

public class OfferClick
{
    public int Id { get; set; }

    public int ProductOfferId { get; set; }

    public ProductOffer? ProductOffer { get; set; }

    public DateTime ClickedAt { get; set; }

    public string? UserAgent { get; set; }

    public string? Referrer { get; set; }
}
