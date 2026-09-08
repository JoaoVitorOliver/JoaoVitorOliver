namespace MegaDescontao.Api.Common;

public static class UrlValidation
{
    /// Só http e https entram no banco. Como o /api/go/{id} redireciona para o que está
    /// gravado, barrar esquemas como javascript: e data: aqui na entrada é o que impede
    /// o redirecionamento de virar vetor de ataque.
    public static bool IsHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
