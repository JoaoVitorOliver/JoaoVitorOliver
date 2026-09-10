using MegaDescontao.Api.Common;
using MegaDescontao.Api.Contracts;
using MegaDescontao.Api.Data;
using MegaDescontao.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MegaDescontao.Api.Endpoints;

public static class ProductEndpoints
{
    private const int MaxPageSize = 48;

    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Produtos");

        group.MapGet("/", ListProducts)
            .WithName("ListProducts")
            .WithSummary("Vitrine: produtos com pelo menos uma oferta ativa, com busca, filtros, ordenação e paginação.");

        group.MapGet("/{id:int}", GetProduct)
            .WithName("GetProduct")
            .WithSummary("Detalhe do produto com todas as ofertas ativas e o histórico de preço dos últimos 30 dias.");

        return app;
    }

    private static async Task<IResult> ListProducts(
        AppDbContext db,
        string? search,
        string? category,
        string? store,
        string? sort,
        int? minDiscount,
        int page = 1,
        int pageSize = 12)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var now = DateTime.UtcNow;

        var query = db.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.Offers.Any(o =>
                o.Status == OfferStatus.Active && (o.ExpiresAt == null || o.ExpiresAt > now)));

        if (!string.IsNullOrWhiteSpace(search))
        {
            // SearchText já está sem acento e em minúsculas; o termo passa pelo mesmo
            // tratamento para que "TÊNIS", "tenis" e "Tênis" encontrem a mesma coisa.
            var term = TextSearch.EscapeLike(TextSearch.Normalize(search));
            if (term.Length > 0)
            {
                query = query.Where(p => EF.Functions.Like(p.SearchText, $"%{term}%", "\\"));
            }
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category!.Slug == category);
        }

        if (!string.IsNullOrWhiteSpace(store))
        {
            query = query.Where(p => p.Offers.Any(o =>
                o.Store!.Slug == store &&
                o.Status == OfferStatus.Active &&
                (o.ExpiresAt == null || o.ExpiresAt > now)));
        }

        if (minDiscount is > 0)
        {
            var factor = 1m - (Math.Min(minDiscount.Value, 99) / 100m);
            query = query.Where(p => p.Offers.Any(o =>
                o.Status == OfferStatus.Active &&
                (o.ExpiresAt == null || o.ExpiresAt > now) &&
                o.OriginalPrice != null &&
                o.OriginalPrice > 0 &&
                o.CurrentPrice <= o.OriginalPrice.Value * factor));
        }

        var total = await query.CountAsync();
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

        // Fora da faixa devolve página vazia — e evita que (page - 1) * pageSize estoure o int.
        if (page > totalPages)
        {
            return Results.Ok(new PagedResponse<ProductListItem>([], page, pageSize, total, totalPages));
        }

        var products = await ApplySort(query, sort, now)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Description,
                p.ImageUrl,
                p.CreatedAt,
                CategoryName = p.Category!.Name,
                CategorySlug = p.Category.Slug,
            })
            .ToListAsync();

        var ids = products.Select(p => p.Id).ToList();

        // Uma consulta só para as ofertas da página inteira: nada de N+1.
        var offers = await db.Offers
            .AsNoTracking()
            .Where(o => ids.Contains(o.ProductId) &&
                        o.Status == OfferStatus.Active &&
                        (o.ExpiresAt == null || o.ExpiresAt > now) &&
                        (store == null || o.Store!.Slug == store))
            .Select(o => new
            {
                o.Id,
                o.ProductId,
                o.CurrentPrice,
                o.OriginalPrice,
                o.ClickCount,
                o.ExpiresAt,
                StoreName = o.Store!.Name,
                StoreSlug = o.Store.Slug,
                o.Store.LogoUrl,
            })
            .ToListAsync();

        var offersByProduct = offers
            .GroupBy(o => o.ProductId)
            .ToDictionary(g => g.Key, g => g.OrderBy(o => o.CurrentPrice).ThenBy(o => o.Id).ToList());

        var lowestFlags = await BuildLowestPriceFlagsAsync(
            db,
            offers.ToDictionary(o => o.Id, o => o.CurrentPrice),
            now);

        var items = new List<ProductListItem>(products.Count);

        foreach (var product in products)
        {
            if (!offersByProduct.TryGetValue(product.Id, out var productOffers) || productOffers.Count == 0)
            {
                continue;
            }

            var best = productOffers[0];

            items.Add(new ProductListItem(
                product.Id,
                product.Title,
                product.Description,
                product.ImageUrl,
                new CategorySummary(product.CategoryName, product.CategorySlug),
                new OfferSummary(
                    best.Id,
                    new StoreSummary(best.StoreName, best.StoreSlug, best.LogoUrl),
                    best.CurrentPrice,
                    best.OriginalPrice,
                    Discount(best.CurrentPrice, best.OriginalPrice),
                    best.ClickCount,
                    best.ExpiresAt,
                    lowestFlags.Contains(best.Id)),
                productOffers.Count,
                product.CreatedAt));
        }

        return Results.Ok(new PagedResponse<ProductListItem>(items, page, pageSize, total, totalPages));
    }

    private static async Task<IResult> GetProduct(int id, AppDbContext db)
    {
        var now = DateTime.UtcNow;

        var product = await db.Products
            .AsNoTracking()
            .Where(p => p.Id == id && p.IsActive)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Description,
                p.ImageUrl,
                p.CreatedAt,
                p.UpdatedAt,
                CategoryName = p.Category!.Name,
                CategorySlug = p.Category.Slug,
            })
            .FirstOrDefaultAsync();

        if (product is null)
        {
            return Results.NotFound(new { message = "Produto não encontrado." });
        }

        var offers = await db.Offers
            .AsNoTracking()
            .Where(o => o.ProductId == id &&
                        o.Status == OfferStatus.Active &&
                        (o.ExpiresAt == null || o.ExpiresAt > now))
            .OrderBy(o => o.CurrentPrice)
            .Select(o => new OfferSummary(
                o.Id,
                new StoreSummary(o.Store!.Name, o.Store.Slug, o.Store.LogoUrl),
                o.CurrentPrice,
                o.OriginalPrice,
                0,
                o.ClickCount,
                o.ExpiresAt))
            .ToListAsync();

        offers = offers
            .Select(o => o with { DiscountPercentage = Discount(o.CurrentPrice, o.OriginalPrice) })
            .ToList();

        // Agregação em memória: são poucas linhas para um produto só, e evita depender de
        // tradução de Min/Average sobre decimal convertido para REAL no SQLite.
        var since = now.AddDays(-30);
        var prices = await db.PriceHistory
            .AsNoTracking()
            .Where(h => h.ProductOffer!.ProductId == id && h.CollectedAt >= since)
            .Select(h => h.Price)
            .ToListAsync();

        decimal? lowest = prices.Count > 0 ? prices.Min() : null;
        decimal? average = prices.Count > 0 ? Math.Round(prices.Average(), 2) : null;

        return Results.Ok(new ProductDetail(
            product.Id,
            product.Title,
            product.Description,
            product.ImageUrl,
            new CategorySummary(product.CategoryName, product.CategorySlug),
            offers,
            lowest,
            average,
            product.CreatedAt,
            product.UpdatedAt));
    }

    private static IOrderedQueryable<Product> ApplySort(IQueryable<Product> query, string? sort, DateTime now) =>
        sort switch
        {
            "menor-preco" => query
                .OrderBy(p => p.Offers
                    .Where(o => o.Status == OfferStatus.Active && (o.ExpiresAt == null || o.ExpiresAt > now))
                    .OrderBy(o => o.CurrentPrice)
                    .Select(o => o.CurrentPrice)
                    .FirstOrDefault())
                .ThenByDescending(p => p.Id),

            "maior-preco" => query
                .OrderByDescending(p => p.Offers
                    .Where(o => o.Status == OfferStatus.Active && (o.ExpiresAt == null || o.ExpiresAt > now))
                    .OrderBy(o => o.CurrentPrice)
                    .Select(o => o.CurrentPrice)
                    .FirstOrDefault())
                .ThenByDescending(p => p.Id),

            "maior-desconto" => query
                .OrderByDescending(p => p.Offers
                    .Where(o => o.Status == OfferStatus.Active &&
                                (o.ExpiresAt == null || o.ExpiresAt > now) &&
                                o.OriginalPrice != null &&
                                o.OriginalPrice > 0)
                    .Select(o => (o.OriginalPrice!.Value - o.CurrentPrice) / o.OriginalPrice.Value)
                    .OrderByDescending(percent => percent)
                    .FirstOrDefault())
                .ThenByDescending(p => p.Id),

            "populares" => query
                .OrderByDescending(p => p.Offers.Sum(o => o.ClickCount))
                .ThenByDescending(p => p.Id),

            _ => query
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id),
        };

    /// Descobre, numa consulta só para a página inteira, quais ofertas estão no menor preço
    /// dos últimos 30 dias. A comparação acontece em memória: são poucas linhas por oferta, e
    /// assim não dependemos de tradução de Min/Max sobre decimal convertido no SQLite.
    private static async Task<HashSet<int>> BuildLowestPriceFlagsAsync(
        AppDbContext db,
        IReadOnlyDictionary<int, decimal> currentPrices,
        DateTime now)
    {
        var flags = new HashSet<int>();

        if (currentPrices.Count == 0)
        {
            return flags;
        }

        var offerIds = currentPrices.Keys.ToList();
        var since = now.AddDays(-30);

        var history = await db.PriceHistory
            .AsNoTracking()
            .Where(h => offerIds.Contains(h.ProductOfferId) && h.CollectedAt >= since)
            .Select(h => new { h.ProductOfferId, h.Price })
            .ToListAsync();

        foreach (var group in history.GroupBy(h => h.ProductOfferId))
        {
            var prices = group.Select(h => h.Price).ToList();

            // Um preço só observado não prova nada, e se a oferta nunca esteve mais cara
            // também não há notícia a dar. O selo só sai quando o histórico sustenta.
            if (prices.Count < 2)
            {
                continue;
            }

            var current = currentPrices[group.Key];

            if (prices.Max() > current && current <= prices.Min())
            {
                flags.Add(group.Key);
            }
        }

        return flags;
    }

    private static int Discount(decimal current, decimal? original) =>
        original > 0 && original > current
            ? (int)Math.Round((1 - current / original.Value) * 100)
            : 0;
}
