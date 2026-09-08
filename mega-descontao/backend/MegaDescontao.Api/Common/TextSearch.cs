using System.Globalization;
using System.Text;

namespace MegaDescontao.Api.Common;

public static class TextSearch
{
    /// Deixa o texto em minúsculas e sem acento, para que "TÊNIS", "tenis" e "Tênis"
    /// caiam todos no mesmo termo comparável.
    public static string Normalize(params string?[] parts)
    {
        var joined = string.Join(' ', parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        if (joined.Length == 0)
        {
            return string.Empty;
        }

        var decomposed = joined.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    /// Neutraliza os curingas do LIKE para que quem procura "50%" não receba o catálogo inteiro.
    public static string EscapeLike(string term) => term
        .Replace("\\", "\\\\")
        .Replace("%", "\\%")
        .Replace("_", "\\_");

    public static string Slugify(string value)
    {
        var normalized = Normalize(value);
        var builder = new StringBuilder(normalized.Length);
        var lastWasDash = false;

        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                lastWasDash = false;
            }
            else if (!lastWasDash && builder.Length > 0)
            {
                builder.Append('-');
                lastWasDash = true;
            }
        }

        return builder.ToString().Trim('-');
    }
}
