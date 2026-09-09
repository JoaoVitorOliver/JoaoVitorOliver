using System.Text.Json;
using System.Text.Json.Serialization;

namespace MegaDescontao.Api.Marketplaces.Providers.Shopee;

/// Uma oferta como a Shopee devolve. Ids e preços vêm ora como número, ora como string
/// dependendo do campo, então tudo numérico é lido com AllowReadingFromString.
public sealed class ShopeeOfferNode
{
    public long ItemId { get; set; }

    public long ShopId { get; set; }

    public string? ProductName { get; set; }

    public string? ImageUrl { get; set; }

    public decimal? PriceMin { get; set; }

    public decimal? PriceMax { get; set; }

    /// Percentual de desconto (ex.: 55 para 55%). É a única pista de "preço de antes".
    public decimal? PriceDiscountRate { get; set; }

    /// Link já rastreado com o identificador do afiliado. É o que vai para o banco.
    public string? OfferLink { get; set; }

    public string? ProductLink { get; set; }

    public string? ShopName { get; set; }

    public List<long>? ProductCatIds { get; set; }

    /// Início e fim da promoção, em segundos desde a época Unix. 0 significa sem prazo.
    public long PeriodStartTime { get; set; }

    public long PeriodEndTime { get; set; }
}

public sealed class ShopeePageInfo
{
    public int Page { get; set; }

    public int Limit { get; set; }

    public bool HasNextPage { get; set; }
}

public sealed class ShopeeProductOfferResult
{
    public List<ShopeeOfferNode>? Nodes { get; set; }

    public ShopeePageInfo? PageInfo { get; set; }
}

public sealed class ShopeeData
{
    [JsonPropertyName("productOfferV2")]
    public ShopeeProductOfferResult? ProductOfferV2 { get; set; }
}

public sealed class ShopeeError
{
    public string? Message { get; set; }
}

public sealed class ShopeeGraphQlResponse
{
    public ShopeeData? Data { get; set; }

    public List<ShopeeError>? Errors { get; set; }
}

public static class ShopeeJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };
}
