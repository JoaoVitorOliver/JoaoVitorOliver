using System.Security.Claims;
using MegaDescontao.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace MegaDescontao.Api.Endpoints;

public record CurrentUserResponse(string Email, bool IsAdmin);

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Conta");

        // Registro, login, troca de senha e afins vêm prontos do Identity. Escrever isso
        // à mão é onde projetos vazam senha; aqui o único trabalho é configurar.
        group.MapIdentityApi<AppUser>();

        group.MapGet("/me", GetCurrentUser)
            .WithSummary("Quem está logado. Responde 401 para visitante anônimo.")
            .RequireAuthorization();

        group.MapPost("/logout", Logout)
            .WithSummary("Encerra a sessão e apaga o cookie.")
            .RequireAuthorization();

        return app;
    }

    private static async Task<IResult> GetCurrentUser(ClaimsPrincipal principal, UserManager<AppUser> users)
    {
        var user = await users.GetUserAsync(principal);

        if (user is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new CurrentUserResponse(
            user.Email ?? string.Empty,
            await users.IsInRoleAsync(user, AppRoles.Admin)));
    }

    private static async Task<IResult> Logout(SignInManager<AppUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.Ok();
    }
}
