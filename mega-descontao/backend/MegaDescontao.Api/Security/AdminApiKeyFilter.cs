using System.Security.Cryptography;
using System.Text;

namespace MegaDescontao.Api.Security;

/// Proteção das rotas administrativas. Sem chave configurada as rotas ficam DESLIGADAS
/// em vez de abertas: esquecer de configurar não pode virar catálogo editável por qualquer um.
public class AdminApiKeyFilter(IConfiguration configuration, ILogger<AdminApiKeyFilter> logger) : IEndpointFilter
{
    public const string HeaderName = "X-Admin-Key";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var expected = configuration["Admin:ApiKey"];

        if (string.IsNullOrWhiteSpace(expected))
        {
            logger.LogWarning("Rota administrativa recusada: Admin:ApiKey não está configurada.");

            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Administração desativada",
                detail: "Configure Admin:ApiKey (user-secrets ou variável de ambiente) para habilitar as rotas administrativas.");
        }

        var provided = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (!IsValid(provided, expected))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Não autorizado",
                detail: $"Informe a chave administrativa no cabeçalho {HeaderName}.");
        }

        return await next(context);
    }

    private static bool IsValid(string provided, string expected) =>
        !string.IsNullOrEmpty(provided) &&
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided),
            Encoding.UTF8.GetBytes(expected));
}
