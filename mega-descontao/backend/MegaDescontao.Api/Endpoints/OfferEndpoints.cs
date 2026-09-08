using MegaDescontao.Api.Contracts;
using MegaDescontao.Api.Data;
using MegaDescontao.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MegaDescontao.Api.Endpoints;

public static class OfferEndpoints
{
    private const int MaxPageSize = 48;

    public static IEndpointRouteBuilder MapOfferEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/offers").WithTags("Ofertas");

        group.MapGet("/", ListOffers)
            .WithName("ListOffers")
            .WithSummary("Lista as ofertas ativas da vitrine, com busca, filtros, ordenação e paginação.");

        group.MapGet("/filters", GetFilters)
            .WithName("GetFilters")
            .WithSummary("Lojas e categorias disponíveis para montar os filtros da vitrine.");

        group.MapGet("/{id:int}", GetOffer)
            .WithName("GetOffer")
            .WithSummary("Detalhe de uma oferta.");

        group.MapPost("/", CreateOffer)
            .WithName("CreateOffer")
            .WithSummary("Cadastra uma oferta. Enquanto não existe painel admin, este é o caminho para alimentar o catálogo.");

        group.MapPut("/{id:int}", UpdateOffer)
            .WithName("UpdateOffer")
            .WithSummary("Atualiza uma oferta existente.");

        group.MapDelete("/{id:int}", DeleteOffer)
            .WithName("DeleteOffer")
            .WithSummary("Remove uma oferta e o histórico de cliques dela.");

        return app;
    }

    private static async Task<IResult> ListOffers(
        AppDbContext db,
        string? search,
        string? store,
        string? category,
        string? sort,
        int page = 1,
        int pageSize = 12)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = db.Offers.AsNoTracking().Where(o => o.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(o =>
                EF.Functions.Like(o.Title, $"%{term}%") ||
                (o.Description != null && EF.Functions.Like(o.Description, $"%{term}%")));
        }

        if (!string.IsNullOrWhiteSpace(store))
        {
            query = query.Where(o => o.Store == store);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(o => o.Category == category);
        }

        var total = await query.CountAsync();

        IOrderedQueryable<Offer> ordered = sort switch
        {
            "menor-preco" => query.OrderBy(o => o.Price),
            "maior-preco" => query.OrderByDescending(o => o.Price),
            "maior-desconto" => query.OrderByDescending(o =>
                o.OriginalPrice != null && o.OriginalPrice > 0
                    ? (o.OriginalPrice.Value - o.Price) / o.OriginalPrice.Value
                    : 0),
            "populares" => query.OrderByDescending(o => o.ClickCount),
            _ => query.OrderByDescending(o => o.CreatedAt)
        };

        var offers = await ordered
            .ThenByDescending(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

        return Results.Ok(new PagedResponse<OfferResponse>(
            offers.Select(ToResponse).ToList(),
            page,
            pageSize,
            total,
            totalPages));
    }

    private static async Task<IResult> GetFilters(AppDbContext db)
    {
        var active = db.Offers.AsNoTracking().Where(o => o.IsActive);

        var stores = await active.Select(o => o.Store).Distinct().OrderBy(s => s).ToListAsync();
        var categories = await active.Select(o => o.Category).Distinct().OrderBy(c => c).ToListAsync();

        return Results.Ok(new FiltersResponse(stores, categories));
    }

    private static async Task<IResult> GetOffer(int id, AppDbContext db)
    {
        var offer = await db.Offers.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);

        return offer is null
            ? Results.NotFound(new { message = "Oferta não encontrada." })
            : Results.Ok(ToResponse(offer));
    }

    private static async Task<IResult> CreateOffer(OfferRequest request, AppDbContext db)
    {
        var errors = Validate(request);
        if (errors is not null)
        {
            return Results.ValidationProblem(errors);
        }

        var offer = new Offer
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            ImageUrl = request.ImageUrl?.Trim() ?? string.Empty,
            Price = request.Price,
            OriginalPrice = request.OriginalPrice,
            Store = request.Store.Trim(),
            Category = request.Category.Trim(),
            AffiliateUrl = request.AffiliateUrl.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        db.Offers.Add(offer);
        await db.SaveChangesAsync();

        return Results.Created($"/api/offers/{offer.Id}", ToResponse(offer));
    }

    private static async Task<IResult> UpdateOffer(int id, OfferRequest request, AppDbContext db)
    {
        var errors = Validate(request);
        if (errors is not null)
        {
            return Results.ValidationProblem(errors);
        }

        var offer = await db.Offers.FirstOrDefaultAsync(o => o.Id == id);
        if (offer is null)
        {
            return Results.NotFound(new { message = "Oferta não encontrada." });
        }

        offer.Title = request.Title.Trim();
        offer.Description = request.Description?.Trim();
        offer.ImageUrl = request.ImageUrl?.Trim() ?? string.Empty;
        offer.Price = request.Price;
        offer.OriginalPrice = request.OriginalPrice;
        offer.Store = request.Store.Trim();
        offer.Category = request.Category.Trim();
        offer.AffiliateUrl = request.AffiliateUrl.Trim();
        offer.IsActive = request.IsActive;

        await db.SaveChangesAsync();

        return Results.Ok(ToResponse(offer));
    }

    private static async Task<IResult> DeleteOffer(int id, AppDbContext db)
    {
        var offer = await db.Offers.FirstOrDefaultAsync(o => o.Id == id);
        if (offer is null)
        {
            return Results.NotFound(new { message = "Oferta não encontrada." });
        }

        db.Offers.Remove(offer);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static OfferResponse ToResponse(Offer offer) => new(
        offer.Id,
        offer.Title,
        offer.Description,
        offer.ImageUrl,
        offer.Price,
        offer.OriginalPrice,
        offer.DiscountPercentage,
        offer.Store,
        offer.Category,
        offer.ClickCount,
        offer.IsActive,
        offer.CreatedAt);

    private static Dictionary<string, string[]>? Validate(OfferRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors[nameof(request.Title)] = ["Informe o título da oferta."];
        }

        if (string.IsNullOrWhiteSpace(request.Store))
        {
            errors[nameof(request.Store)] = ["Informe a loja (ex.: Mercado Livre, Shopee, Temu)."];
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            errors[nameof(request.Category)] = ["Informe a categoria da oferta."];
        }

        if (request.Price <= 0)
        {
            errors[nameof(request.Price)] = ["O preço promocional deve ser maior que zero."];
        }

        if (request.OriginalPrice is not null && request.OriginalPrice <= request.Price)
        {
            errors[nameof(request.OriginalPrice)] = ["O preço original deve ser maior que o preço promocional."];
        }

        // O destino do /api/go/{id} vem daqui: barrar esquemas como javascript: já na entrada
        // é o que impede o redirect de virar um vetor de ataque.
        if (!IsHttpUrl(request.AffiliateUrl))
        {
            errors[nameof(request.AffiliateUrl)] = ["Informe um link de afiliado válido começando com http:// ou https://."];
        }

        if (!string.IsNullOrWhiteSpace(request.ImageUrl) && !IsHttpUrl(request.ImageUrl))
        {
            errors[nameof(request.ImageUrl)] = ["A URL da imagem deve começar com http:// ou https://."];
        }

        return errors.Count == 0 ? null : errors;
    }

    private static bool IsHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
