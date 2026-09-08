using MegaDescontao.Api.Contracts;
using MegaDescontao.Api.Data;
using MegaDescontao.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MegaDescontao.Api.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/categories", GetCategories)
            .WithName("ListCategories")
            .WithTags("Catálogo")
            .WithSummary("Categorias que hoje têm ao menos um produto com oferta ativa.");

        app.MapGet("/api/stores", GetStores)
            .WithName("ListStores")
            .WithTags("Catálogo")
            .WithSummary("Lojas que hoje têm ao menos uma oferta ativa.");

        return app;
    }

    private static async Task<IResult> GetCategories(AppDbContext db)
    {
        var now = DateTime.UtcNow;

        var categories = await db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive && c.Products.Any(p => p.IsActive && p.Offers.Any(o =>
                o.Status == OfferStatus.Active && (o.ExpiresAt == null || o.ExpiresAt > now))))
            .OrderBy(c => c.Name)
            .Select(c => new CategorySummary(c.Name, c.Slug))
            .ToListAsync();

        return Results.Ok(categories);
    }

    private static async Task<IResult> GetStores(AppDbContext db)
    {
        var now = DateTime.UtcNow;

        var stores = await db.Stores
            .AsNoTracking()
            .Where(s => s.IsActive && s.Offers.Any(o =>
                o.Status == OfferStatus.Active &&
                (o.ExpiresAt == null || o.ExpiresAt > now) &&
                o.Product!.IsActive))
            .OrderBy(s => s.Name)
            .Select(s => new StoreSummary(s.Name, s.Slug, s.LogoUrl))
            .ToListAsync();

        return Results.Ok(stores);
    }
}
