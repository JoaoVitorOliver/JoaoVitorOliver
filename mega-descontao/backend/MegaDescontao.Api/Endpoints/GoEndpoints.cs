using MegaDescontao.Api.Data;
using MegaDescontao.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MegaDescontao.Api.Endpoints;

public static class GoEndpoints
{
    private const int MaxUserAgentLength = 400;
    private const int MaxReferrerLength = 600;

    public static IEndpointRouteBuilder MapGoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/go/{id:int}", RedirectToAffiliate)
            .WithName("GoToOffer")
            .WithTags("Redirecionamento")
            .WithSummary("Registra o clique na oferta e redireciona para o link de afiliado.");

        return app;
    }

    private static async Task<IResult> RedirectToAffiliate(
        int id,
        AppDbContext db,
        HttpContext http,
        ILoggerFactory loggerFactory)
    {
        var offer = await db.Offers.FirstOrDefaultAsync(o => o.Id == id && o.IsActive);
        if (offer is null)
        {
            return Results.NotFound(new { message = "Oferta não encontrada ou fora do ar." });
        }

        db.Clicks.Add(new OfferClick
        {
            OfferId = offer.Id,
            ClickedAt = DateTime.UtcNow,
            UserAgent = Truncate(http.Request.Headers.UserAgent.ToString(), MaxUserAgentLength),
            Referrer = Truncate(http.Request.Headers.Referer.ToString(), MaxReferrerLength)
        });
        offer.ClickCount++;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Perder a estatística é bem menos grave do que perder a venda:
            // se a gravação falhar, o usuário segue para a loja mesmo assim.
            loggerFactory.CreateLogger(nameof(GoEndpoints))
                .LogError(ex, "Falha ao registrar o clique da oferta {OfferId}", offer.Id);
        }

        return Results.Redirect(offer.AffiliateUrl, permanent: false);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
