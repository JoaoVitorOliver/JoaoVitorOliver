namespace MegaDescontao.Api.Models;

/// O produto em si, independente de onde ele é vendido. O mesmo iPhone pode ter
/// uma oferta no Mercado Livre e outra na Shopee — cada uma é um ProductOffer.
public class Product
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public Category? Category { get; set; }

    public bool IsActive { get; set; } = true;

    /// Título e descrição sem acento e em minúsculas. Existe porque o LIKE do SQLite
    /// só faz case-folding em ASCII: sem isso, buscar "tenis" não acha "Tênis".
    public string SearchText { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<ProductOffer> Offers { get; set; } = [];
}
