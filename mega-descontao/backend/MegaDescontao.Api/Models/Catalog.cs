namespace MegaDescontao.Api.Models;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// Identificador estável usado nas URLs e nos filtros da API (ex.: "casa-e-cozinha").
    public string Slug { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<Product> Products { get; set; } = [];
}

public class Store
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public List<ProductOffer> Offers { get; set; } = [];
}
