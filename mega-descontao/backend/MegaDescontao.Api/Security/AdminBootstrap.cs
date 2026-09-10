using MegaDescontao.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace MegaDescontao.Api.Security;

/// Garante que exista um administrador para entrar na tela de curadoria.
///
/// Em produção, só cria se Admin:Email e Admin:Password estiverem configurados — nunca
/// inventa credencial num site que está no ar. Em desenvolvimento, cria um par conhecido
/// e avisa no log, para não travar quem acabou de clonar o projeto.
public static class AdminBootstrap
{
    public const string DevEmail = "admin@megadescontao.local";
    public const string DevPassword = "Admin@12345";

    public static async Task EnsureAdminAsync(IServiceProvider services, IWebHostEnvironment environment)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        var users = services.GetRequiredService<UserManager<AppUser>>();
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roles.RoleExistsAsync(AppRoles.Admin))
        {
            await roles.CreateAsync(new IdentityRole(AppRoles.Admin));
        }

        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (!environment.IsDevelopment())
            {
                logger.LogWarning(
                    "Nenhum administrador configurado. Defina Admin__Email e Admin__Password para " +
                    "poder entrar na tela de curadoria.");
                return;
            }

            email = DevEmail;
            password = DevPassword;

            logger.LogWarning(
                "Administrador de desenvolvimento: {Email} / {Password}. Só existe fora de produção.",
                email, password);
        }

        var admin = await users.FindByEmailAsync(email);

        if (admin is null)
        {
            admin = new AppUser { UserName = email, Email = email, EmailConfirmed = true };
            var created = await users.CreateAsync(admin, password);

            if (!created.Succeeded)
            {
                logger.LogError(
                    "Não foi possível criar o administrador {Email}: {Erros}",
                    email, string.Join("; ", created.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("Administrador {Email} criado.", email);
        }

        if (!await users.IsInRoleAsync(admin, AppRoles.Admin))
        {
            await users.AddToRoleAsync(admin, AppRoles.Admin);
        }
    }
}
