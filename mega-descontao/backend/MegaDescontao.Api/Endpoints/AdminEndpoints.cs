using MegaDescontao.Api.Common;
using MegaDescontao.Api.Contracts;
using MegaDescontao.Api.Data;
using MegaDescontao.Api.Marketplaces;
using MegaDescontao.Api.Models;
using MegaDescontao.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace MegaDescontao.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Administração")
            .AddEndpointFilter<AdminApiKeyFilter>()
            .RequireRateLimiting(RateLimitPolicies.Admin);

        group.MapGet("/products", ListProducts)
            .WithSummary("Lista o catálogo completo, inclusive produtos inativos e ofertas fora do ar.");

        group.MapGet("/products/{id:int}", GetProduct);
        group.MapPost("/products", CreateProduct);
        group.MapPut("/products/{id:int}", UpdateProduct);
        group.MapDelete("/products/{id:int}", DeleteProduct);

        group.MapPost("/products/{id:int}/offers", AddOffer)
            .WithSummary("Adiciona a oferta de mais um marketplace ao mesmo produto.");

        group.MapPut("/offers/{offerId:int}", UpdateOffer);
        group.MapDelete("/offers/{offerId:int}", DeleteOffer);

        group.MapGet("/clicks", GetClickReport)
            .WithSummary("Cliques por oferta, do mais clicado para o menos clicado.");

        group.MapPost("/categories", CreateCategory);
        group.MapPost("/stores", CreateStore);

        group.MapPost("/import/{storeSlug}", RunImport)
            .WithSummary("Roda o provider da loja e reconcilia o catálogo.");

        group.MapPost("/offers/expire", ExpireOffers)
            .WithSummary("Marca como expiradas as promoções cujo prazo já passou.");

        return app;
    }

    private static async Task<IResult> ListProducts(AppDbContext db, int page = 1, int pageSize = 50)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var total = await db.Products.CountAsync();
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

        if (page > totalPages)
        {
            return Results.Ok(new PagedResponse<AdminProductResponse>([], page, pageSize, total, totalPages));
        }

        var products = await db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Offers).ThenInclude(o => o.Store)
            .OrderByDescending(p => p.UpdatedAt)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Results.Ok(new PagedResponse<AdminProductResponse>(
            products.Select(ToAdminResponse).ToList(), page, pageSize, total, totalPages));
    }

    private static async Task<IResult> GetProduct(int id, AppDbContext db)
    {
        var product = await LoadProductAsync(db, id, tracking: false);

        return product is null
            ? Results.NotFound(new { message = "Produto não encontrado." })
            : Results.Ok(ToAdminResponse(product));
    }

    private static async Task<IResult> CreateProduct(ProductInput input, AppDbContext db)
    {
        var errors = ValidateProduct(input);
        foreach (var offer in input.Offers ?? [])
        {
            MergeOfferErrors(errors, offer);
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Slug == input.CategorySlug);
        if (category is null)
        {
            return UnknownSlug("CategorySlug", input.CategorySlug);
        }

        var now = DateTime.UtcNow;

        var product = new Product
        {
            Title = input.Title.Trim(),
            Description = input.Description?.Trim(),
            ImageUrl = input.ImageUrl?.Trim() ?? string.Empty,
            Category = category,
            IsActive = input.IsActive,
            SearchText = TextSearch.Normalize(input.Title, input.Description),
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var offerInput in input.Offers ?? [])
        {
            var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == offerInput.StoreSlug);
            if (store is null)
            {
                return UnknownSlug("StoreSlug", offerInput.StoreSlug);
            }

            product.Offers.Add(BuildOffer(offerInput, store, now));
        }

        db.Products.Add(product);
        await db.SaveChangesAsync();

        var saved = await LoadProductAsync(db, product.Id, tracking: false);
        return Results.Created($"/api/admin/products/{product.Id}", ToAdminResponse(saved!));
    }

    private static async Task<IResult> UpdateProduct(int id, ProductInput input, AppDbContext db)
    {
        var errors = ValidateProduct(input);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return Results.NotFound(new { message = "Produto não encontrado." });
        }

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Slug == input.CategorySlug);
        if (category is null)
        {
            return UnknownSlug("CategorySlug", input.CategorySlug);
        }

        product.Title = input.Title.Trim();
        product.Description = input.Description?.Trim();
        product.ImageUrl = input.ImageUrl?.Trim() ?? string.Empty;
        product.CategoryId = category.Id;
        product.IsActive = input.IsActive;
        product.SearchText = TextSearch.Normalize(input.Title, input.Description);
        product.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var saved = await LoadProductAsync(db, id, tracking: false);
        return Results.Ok(ToAdminResponse(saved!));
    }

    private static async Task<IResult> DeleteProduct(int id, AppDbContext db)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return Results.NotFound(new { message = "Produto não encontrado." });
        }

        db.Products.Remove(product);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> AddOffer(int id, OfferInput input, AppDbContext db)
    {
        var errors = new Dictionary<string, string[]>();
        MergeOfferErrors(errors, input);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return Results.NotFound(new { message = "Produto não encontrado." });
        }

        var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == input.StoreSlug);
        if (store is null)
        {
            return UnknownSlug("StoreSlug", input.StoreSlug);
        }

        var offer = BuildOffer(input, store, DateTime.UtcNow);
        offer.ProductId = id;

        db.Offers.Add(offer);
        await db.SaveChangesAsync();

        var saved = await LoadProductAsync(db, id, tracking: false);
        return Results.Created($"/api/admin/offers/{offer.Id}", ToAdminResponse(saved!));
    }

    private static async Task<IResult> UpdateOffer(int offerId, OfferInput input, AppDbContext db)
    {
        var errors = new Dictionary<string, string[]>();
        MergeOfferErrors(errors, input);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var offer = await db.Offers.Include(o => o.Store).FirstOrDefaultAsync(o => o.Id == offerId);
        if (offer is null)
        {
            return Results.NotFound(new { message = "Oferta não encontrada." });
        }

        var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == input.StoreSlug);
        if (store is null)
        {
            return UnknownSlug("StoreSlug", input.StoreSlug);
        }

        var now = DateTime.UtcNow;

        // Mudança de preço vira histórico: é o que permite dizer depois se a promoção é boa.
        if (offer.CurrentPrice != input.CurrentPrice)
        {
            db.PriceHistory.Add(new PriceHistory
            {
                ProductOfferId = offer.Id,
                Price = input.CurrentPrice,
                CollectedAt = now,
            });
        }

        offer.StoreId = store.Id;
        offer.AffiliateUrl = input.AffiliateUrl.Trim();
        offer.ProductUrl = input.ProductUrl?.Trim();
        offer.ExternalProductId = input.ExternalProductId?.Trim();
        offer.CurrentPrice = input.CurrentPrice;
        offer.OriginalPrice = input.OriginalPrice;
        offer.Status = input.Status;
        offer.ExpiresAt = input.ExpiresAt;
        offer.UpdatedAt = now;

        await db.SaveChangesAsync();

        var saved = await LoadProductAsync(db, offer.ProductId, tracking: false);
        return Results.Ok(ToAdminResponse(saved!));
    }

    private static async Task<IResult> DeleteOffer(int offerId, AppDbContext db)
    {
        var offer = await db.Offers.FirstOrDefaultAsync(o => o.Id == offerId);
        if (offer is null)
        {
            return Results.NotFound(new { message = "Oferta não encontrada." });
        }

        db.Offers.Remove(offer);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> GetClickReport(AppDbContext db, int limit = 50)
    {
        limit = Math.Clamp(limit, 1, 500);

        var report = await db.Offers
            .AsNoTracking()
            .Where(o => o.ClickCount > 0)
            .OrderByDescending(o => o.ClickCount)
            .Take(limit)
            .Select(o => new ClickReportItem(
                o.Id,
                o.ProductId,
                o.Product!.Title,
                o.Store!.Name,
                o.ClickCount))
            .ToListAsync();

        return Results.Ok(report);
    }

    private static async Task<IResult> CreateCategory(CategorySummary input, AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Name"] = ["Informe o nome da categoria."],
            });
        }

        var slug = string.IsNullOrWhiteSpace(input.Slug) ? TextSearch.Slugify(input.Name) : input.Slug.Trim();

        if (await db.Categories.AnyAsync(c => c.Slug == slug))
        {
            return Results.Conflict(new { message = $"Já existe uma categoria com o slug '{slug}'." });
        }

        var category = new Category { Name = input.Name.Trim(), Slug = slug };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return Results.Created($"/api/categories", new CategorySummary(category.Name, category.Slug));
    }

    private static async Task<IResult> CreateStore(StoreSummary input, AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Name"] = ["Informe o nome da loja."],
            });
        }

        if (!string.IsNullOrWhiteSpace(input.LogoUrl) && !UrlValidation.IsHttpUrl(input.LogoUrl))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["LogoUrl"] = ["A URL do logo deve começar com http:// ou https://."],
            });
        }

        var slug = string.IsNullOrWhiteSpace(input.Slug) ? TextSearch.Slugify(input.Name) : input.Slug.Trim();

        if (await db.Stores.AnyAsync(s => s.Slug == slug))
        {
            return Results.Conflict(new { message = $"Já existe uma loja com o slug '{slug}'." });
        }

        var store = new Store { Name = input.Name.Trim(), Slug = slug, LogoUrl = input.LogoUrl?.Trim() };
        db.Stores.Add(store);
        await db.SaveChangesAsync();

        return Results.Created($"/api/stores", new StoreSummary(store.Name, store.Slug, store.LogoUrl));
    }

    private static async Task<IResult> RunImport(
        string storeSlug,
        IEnumerable<IMarketplaceProvider> providers,
        ProductImportService importService,
        CancellationToken cancellationToken)
    {
        var candidates = providers
            .Where(p => string.Equals(p.StoreSlug, storeSlug, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count == 0)
        {
            return Results.NotFound(new
            {
                message = $"Nenhum provider registrado para '{storeSlug}'.",
                disponiveis = providers.Select(p => p.StoreSlug).Distinct().ToArray(),
            });
        }

        // A mesma loja pode ter o provider oficial e um feed manual registrados ao mesmo tempo.
        // Vence quem estiver configurado — assim o feed JSON cobre o período em que a API
        // oficial ainda não foi liberada, e sai de cena sozinho quando as credenciais chegarem.
        var provider = candidates.FirstOrDefault(p => p.IsConfigured) ?? candidates[0];

        var result = await importService.ImportAsync(provider, cancellationToken);

        return Results.Ok(new ImportResultResponse(
            provider.DisplayName,
            result.StoreSlug,
            result.Received,
            result.Created,
            result.Updated,
            result.PriceChanges,
            result.MarkedUnavailable,
            result.Warnings));
    }

    private static async Task<IResult> ExpireOffers(ProductImportService importService, CancellationToken cancellationToken)
    {
        var expired = await importService.ExpireOutdatedOffersAsync(cancellationToken);
        return Results.Ok(new { expiradas = expired });
    }

    private static Task<Product?> LoadProductAsync(AppDbContext db, int id, bool tracking)
    {
        var query = db.Products
            .Include(p => p.Category)
            .Include(p => p.Offers).ThenInclude(o => o.Store)
            .AsQueryable();

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(p => p.Id == id);
    }

    private static ProductOffer BuildOffer(OfferInput input, Store store, DateTime now) => new()
    {
        Store = store,
        AffiliateUrl = input.AffiliateUrl.Trim(),
        ProductUrl = input.ProductUrl?.Trim(),
        ExternalProductId = input.ExternalProductId?.Trim(),
        CurrentPrice = input.CurrentPrice,
        OriginalPrice = input.OriginalPrice,
        Status = input.Status,
        ExpiresAt = input.ExpiresAt,
        CreatedAt = now,
        UpdatedAt = now,
        PriceHistory = [new PriceHistory { Price = input.CurrentPrice, CollectedAt = now }],
    };

    private static AdminProductResponse ToAdminResponse(Product product) => new(
        product.Id,
        product.Title,
        product.Description,
        product.ImageUrl,
        product.Category?.Slug ?? string.Empty,
        product.IsActive,
        product.Offers
            .OrderBy(o => o.CurrentPrice)
            .Select(o => new AdminOfferResponse(
                o.Id,
                o.Store?.Slug ?? string.Empty,
                o.Store?.Name ?? string.Empty,
                o.AffiliateUrl,
                o.ProductUrl,
                o.ExternalProductId,
                o.CurrentPrice,
                o.OriginalPrice,
                o.DiscountPercentage,
                o.Status,
                o.ExpiresAt,
                o.LastCheckedAt,
                o.ClickCount,
                o.UpdatedAt))
            .ToList(),
        product.CreatedAt,
        product.UpdatedAt);

    private static Dictionary<string, string[]> ValidateProduct(ProductInput input)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(input.Title))
        {
            errors[nameof(input.Title)] = ["Informe o título do produto."];
        }

        if (string.IsNullOrWhiteSpace(input.CategorySlug))
        {
            errors[nameof(input.CategorySlug)] = ["Informe a categoria."];
        }

        if (!string.IsNullOrWhiteSpace(input.ImageUrl) && !UrlValidation.IsHttpUrl(input.ImageUrl))
        {
            errors[nameof(input.ImageUrl)] = ["A URL da imagem deve começar com http:// ou https://."];
        }

        return errors;
    }

    private static void MergeOfferErrors(Dictionary<string, string[]> errors, OfferInput input)
    {
        if (string.IsNullOrWhiteSpace(input.StoreSlug))
        {
            errors[nameof(input.StoreSlug)] = ["Informe a loja da oferta."];
        }

        if (!UrlValidation.IsHttpUrl(input.AffiliateUrl))
        {
            errors[nameof(input.AffiliateUrl)] = ["Informe um link de afiliado válido começando com http:// ou https://."];
        }

        if (!string.IsNullOrWhiteSpace(input.ProductUrl) && !UrlValidation.IsHttpUrl(input.ProductUrl))
        {
            errors[nameof(input.ProductUrl)] = ["A URL do produto deve começar com http:// ou https://."];
        }

        if (input.CurrentPrice <= 0)
        {
            errors[nameof(input.CurrentPrice)] = ["O preço promocional deve ser maior que zero."];
        }

        if (input.OriginalPrice is not null && input.OriginalPrice <= input.CurrentPrice)
        {
            errors[nameof(input.OriginalPrice)] = ["O preço original deve ser maior que o preço promocional."];
        }
    }

    private static IResult UnknownSlug(string field, string value) =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [field] = [$"'{value}' não existe. Cadastre antes em /api/admin/categories ou /api/admin/stores."],
        });
}
