namespace MegaDescontao.Api.Models;

public class OfferClick
{
    public int Id { get; set; }

    public int OfferId { get; set; }

    public Offer? Offer { get; set; }

    public DateTime ClickedAt { get; set; } = DateTime.UtcNow;

    public string? UserAgent { get; set; }

    public string? Referrer { get; set; }
}
