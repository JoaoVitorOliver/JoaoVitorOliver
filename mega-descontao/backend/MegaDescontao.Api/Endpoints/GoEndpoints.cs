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
        app.MapGet("/api/go/{offerId:int}", RedirectToAffiliate)
            .WithName("GoToOffer")
            .WithTags("Redirecionamento")
            .WithSummary("Registra o clique e redireciona para o link de afiliado da oferta.")
            .RequireRateLimiting(RateLimitPolicies.Redirect);

        return app;
    }

    private static async Task<IResult> RedirectToAffiliate(
        int offerId,
        AppDbContext db,
        HttpContext http,
        ILogger<Program> logger)
    {
        var now = DateTime.UtcNow;

        // O destino vem exclusivamente do banco, endereçado por um id inteiro: o cliente
        // nunca informa URL nenhuma, então não há como transformar isso em open redirect.
        var offer = await db.Offers
            .AsNoTracking()
            .Where(o => o.Id == offerId &&
                        o.Status == OfferStatus.Active &&
                        (o.ExpiresAt == null || o.ExpiresAt > now) &&
                        o.Product!.IsActive)
            .Select(o => new { o.Id, o.AffiliateUrl })
            .FirstOrDefaultAsync();

        if (offer is null)
        {
            return Results.NotFound(new { message = "Oferta não encontrada ou fora do ar." });
        }

        try
        {
            db.Clicks.Add(new OfferClick
            {
                ProductOfferId = offer.Id,
                ClickedAt = now,
                UserAgent = Truncate(http.Request.Headers.UserAgent.ToString(), MaxUserAgentLength),
                Referrer = Truncate(http.Request.Headers.Referer.ToString(), MaxReferrerLength),
            });

            await db.SaveChangesAsync();

            // Incremento feito pelo banco (UPDATE ... SET ClickCount = ClickCount + 1).
            // Ler e reescrever em memória perderia cliques simultâneos na mesma oferta:
            // dois requests leem o mesmo valor e ambos gravam o mesmo +1.
            await db.Offers
                .Where(o => o.Id == offer.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.ClickCount, o => o.ClickCount + 1));
        }
        catch (Exception ex)
        {
            // Perder a estatística custa menos que perder a venda: o usuário segue para a loja.
            logger.LogError(ex, "Falha ao registrar o clique da oferta {OfferId}", offer.Id);
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
