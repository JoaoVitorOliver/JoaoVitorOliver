using Microsoft.AspNetCore.Identity;

namespace MegaDescontao.Api.Models;

/// Usuário do site. Herda do Identity para não escrever hash de senha, bloqueio por
/// tentativa e confirmação de e-mail na mão — é o tipo de código onde errar é caro
/// e o erro só aparece quando alguém já explorou.
public class AppUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class AppRoles
{
    /// Quem pode curar o catálogo. O acesso administrativo continua aceitando a chave de
    /// API para automação; o papel é o que permite entrar pela tela.
    public const string Admin = "Admin";
}
