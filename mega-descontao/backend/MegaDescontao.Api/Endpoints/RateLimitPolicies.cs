namespace MegaDescontao.Api.Endpoints;

public static class RateLimitPolicies
{
    /// Protege o redirecionamento de cliques automatizados que inflariam as métricas.
    public const string Redirect = "redirect";

    /// Protege as rotas administrativas de tentativa de força bruta na chave.
    public const string Admin = "admin";
}
