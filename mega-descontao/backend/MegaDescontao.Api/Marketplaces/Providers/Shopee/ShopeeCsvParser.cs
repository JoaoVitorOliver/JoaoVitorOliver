using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MegaDescontao.Api.Marketplaces.Providers.Shopee;

/// Lê o CSV que a Plataforma de Afiliados da Shopee gera em "Oferta de Produto → Obter Link".
///
/// Cabeçalho observado:
///   Item Id, Item Name, Price, Sales, Nome da loja, Commission Rate, Commission,
///   Product Link, Offer Link
///
/// O arquivo NÃO traz foto, preço original nem categoria — o que vier em branco fica em
/// branco, sem inventar dado. É a diferença entre este caminho e a Open API.
public static partial class ShopeeCsvParser
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    [GeneratedRegex(@"/product/(\d+)/(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex ProductLinkPattern();

    public static IReadOnlyList<MarketplaceOffer> Parse(string csv, string categoryName)
    {
        var rows = ReadRows(csv);
        if (rows.Count < 2)
        {
            return [];
        }

        var columns = MapColumns(rows[0]);
        var offers = new List<MarketplaceOffer>();

        foreach (var row in rows.Skip(1))
        {
            var offer = ToOffer(row, columns, categoryName);
            if (offer is not null)
            {
                offers.Add(offer);
            }
        }

        return offers;
    }

    private static MarketplaceOffer? ToOffer(
        IReadOnlyList<string> row,
        IReadOnlyDictionary<string, int> columns,
        string categoryName)
    {
        var affiliateUrl = Field(row, columns, "Offer Link");
        var title = Field(row, columns, "Item Name");
        var price = ParsePrice(Field(row, columns, "Price"));

        // Sem link de afiliado a linha não serve: é justamente o que o site precisa entregar.
        if (string.IsNullOrWhiteSpace(affiliateUrl) || string.IsNullOrWhiteSpace(title) || price is null or <= 0)
        {
            return null;
        }

        var productUrl = Field(row, columns, "Product Link");
        var itemId = Field(row, columns, "Item Id");

        return new MarketplaceOffer(
            ExternalProductId: BuildExternalId(productUrl, itemId),
            Title: title.Trim(),
            Description: null,
            // O CSV não traz imagem. Fica vazio e o card usa o placeholder até alguém
            // preencher pelo admin ou a Open API trazer a foto.
            ImageUrl: string.Empty,
            CurrentPrice: price.Value,
            // Também não traz "preço de antes", então não há desconto a exibir.
            OriginalPrice: null,
            ProductUrl: string.IsNullOrWhiteSpace(productUrl) ? affiliateUrl.Trim() : productUrl.Trim(),
            AffiliateUrl: affiliateUrl.Trim(),
            CategoryName: categoryName,
            ExpiresAt: null);
    }

    /// Usa shopId-itemId, o mesmo formato que o provider da Open API produz. Assim, quando a
    /// API for liberada, ela ATUALIZA as ofertas importadas por CSV em vez de duplicá-las.
    public static string BuildExternalId(string? productUrl, string? itemId)
    {
        var match = ProductLinkPattern().Match(productUrl ?? string.Empty);

        if (match.Success)
        {
            return $"{match.Groups[1].Value}-{match.Groups[2].Value}";
        }

        return string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim();
    }

    /// "14,99" e "R$2,10" viram 14.99 e 2.10. O separador é o brasileiro, e valores acima de
    /// mil vêm com ponto de milhar ("1.148,98"), então parsear com InvariantCulture erraria feio.
    public static decimal? ParsePrice(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var cleaned = raw.Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

        return decimal.TryParse(cleaned, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, PtBr, out var value)
            ? value
            : null;
    }

    private static string? Field(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> columns, string name) =>
        columns.TryGetValue(name, out var index) && index < row.Count ? row[index] : null;

    private static Dictionary<string, int> MapColumns(IReadOnlyList<string> header)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < header.Count; i++)
        {
            var name = header[i].Trim().TrimStart('﻿');
            if (name.Length > 0)
            {
                map[name] = i;
            }
        }

        return map;
    }

    /// Leitor de CSV no formato RFC 4180: aspas protegem vírgulas dentro do campo
    /// (que é o caso de "14,99") e aspas duplicadas viram uma aspa literal.
    private static List<List<string>> ReadRows(string csv)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < csv.Length; i++)
        {
            var c = csv[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }

                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    break;

                case ',':
                    row.Add(field.ToString());
                    field.Clear();
                    break;

                case '\r':
                    break;

                case '\n':
                    row.Add(field.ToString());
                    field.Clear();
                    AddRow(rows, row);
                    row = [];
                    break;

                default:
                    field.Append(c);
                    break;
            }
        }

        row.Add(field.ToString());
        AddRow(rows, row);

        return rows;
    }

    private static void AddRow(List<List<string>> rows, List<string> row)
    {
        // Linha em branco no fim do arquivo não é registro.
        if (row.Any(cell => !string.IsNullOrWhiteSpace(cell)))
        {
            rows.Add([.. row]);
        }
    }
}
