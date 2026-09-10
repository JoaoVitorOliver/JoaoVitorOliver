using System.Security.Cryptography;
using System.Text;
using MegaDescontao.Api.Models;

namespace MegaDescontao.Api.Security;

/// Proteção das rotas administrativas. Sem chave configurada as rotas ficam DESLIGADAS
/// em vez de abertas: esquecer de configurar não pode virar catálogo editável por qualquer um.
public class AdminApiKeyFilter(IConfiguration configuration, ILogger<AdminApiKeyFilter> logger) : IEndpointFilter
{
    public const string HeaderName = "X-Admin-Key";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Dois caminhos para a mesma porta: a pessoa logada como administrador (tela de
        // curadoria) e a chave de API (automação, scripts, o upload de CSV por curl).
        if (context.HttpContext.User.IsInRole(AppRoles.Admin))
        {
            return await next(context);
        }

        var expected = configuration["Admin:ApiKey"];
        var provided = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (!string.IsNullOrWhiteSpace(expected) && IsValid(provided, expected))
        {
            return await next(context);
        }

        if (string.IsNullOrWhiteSpace(expected))
        {
            logger.LogWarning(
                "Rota administrativa recusada: sem sessão de administrador e sem Admin:ApiKey configurada.");
        }

        return Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Não autorizado",
            detail: $"Entre como administrador ou informe a chave no cabeçalho {HeaderName}.");
    }

    private static bool IsValid(string provided, string expected) =>
        !string.IsNullOrEmpty(provided) &&
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided),
            Encoding.UTF8.GetBytes(expected));
}
