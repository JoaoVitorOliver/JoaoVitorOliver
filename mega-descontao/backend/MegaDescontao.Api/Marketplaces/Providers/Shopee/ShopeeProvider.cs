using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MegaDescontao.Api.Marketplaces.Providers.Shopee;

/// Integração com a Affiliate Open API da Shopee (GraphQL).
///
/// É a via que resolve dado e monetização na mesma chamada: a resposta já traz o
/// `offerLink` rastreado com o identificador do afiliado, então não existe passo manual
/// de gerar link. Depende de conta de afiliado aprovada e de acesso à API liberado.
public class ShopeeProvider(
    ShopeeOptions options,
    IHttpClientFactory httpClientFactory,
    ILogger<ShopeeProvider> logger) : IMarketplaceProvider
{
    public const string HttpClientName = "shopee-affiliate";

    /// Um lugar só para ajustar se a Shopee mudar o schema. Campos a mais que a API não
    /// conheça derrubam a query inteira, então a lista é conservadora de propósito.
    private const string OffersQuery = """
        query ProductOffers($page: Int, $limit: Int) {
          productOfferV2(page: $page, limit: $limit) {
            nodes {
              itemId
              shopId
              productName
              imageUrl
              priceMin
              priceMax
              priceDiscountRate
              offerLink
              productLink
              shopName
              productCatIds
              periodStartTime
              periodEndTime
            }
            pageInfo {
              page
              limit
              hasNextPage
            }
          }
        }
        """;

    public string StoreSlug => "shopee";

    public string DisplayName => "Shopee";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(options.ClientId) &&
        !string.IsNullOrWhiteSpace(options.ClientSecret);

    /// A API devolve uma seleção de ofertas, não o catálogo inteiro da loja: uma oferta
    /// ausente da resposta não significa que saiu do ar.
    public bool ProvidesFullSnapshot => false;

    public async Task<IReadOnlyList<MarketplaceOffer>> FetchOffersAsync(CancellationToken cancellationToken)
    {
        var collected = new List<MarketplaceOffer>();
        var maxOffers = Math.Max(options.MaxOffers, 1);
        var pageSize = Math.Clamp(options.PageSize, 1, 100);
        var page = 1;

        while (collected.Count < maxOffers && !cancellationToken.IsCancellationRequested)
        {
            var result = await FetchPageAsync(page, pageSize, cancellationToken);
            var nodes = result?.Nodes ?? [];

            if (nodes.Count == 0)
            {
                break;
            }

            foreach (var node in nodes)
            {
                var offer = ShopeeOfferMapper.ToMarketplaceOffer(node, options);

                if (offer is null)
                {
                    logger.LogDebug("Oferta {ShopId}-{ItemId} ignorada: faltou link, nome ou preço.",
                        node.ShopId, node.ItemId);
                    continue;
                }

                collected.Add(offer);

                if (collected.Count >= maxOffers)
                {
                    break;
                }
            }

            if (result?.PageInfo?.HasNextPage != true || collected.Count >= maxOffers)
            {
                break;
            }

            page++;

            if (options.DelayBetweenPagesMs > 0)
            {
                await Task.Delay(options.DelayBetweenPagesMs, cancellationToken);
            }
        }

        logger.LogInformation("Shopee: {Count} oferta(s) prontas para importar.", collected.Count);

        return collected;
    }

    private async Task<ShopeeProductOfferResult?> FetchPageAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        // O corpo é serializado UMA vez e essa mesma string é assinada e enviada. Serializar
        // de novo para enviar produziria uma assinatura que não confere.
        var payload = JsonSerializer.Serialize(new
        {
            query = OffersQuery,
            variables = new { page, limit = pageSize },
        });

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = ShopeeSignature.Compute(options.ClientId!, timestamp, payload, options.ClientSecret!);

        using var request = new HttpRequestMessage(HttpMethod.Post, options.Endpoint)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };

        request.Headers.TryAddWithoutValidation(
            "Authorization",
            ShopeeSignature.BuildAuthorizationHeader(options.ClientId!, timestamp, signature));

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // O corpo carrega o código de erro da Shopee (assinatura inválida, sem permissão
            // de API, limite estourado). Sem ele, o diagnóstico vira adivinhação.
            throw new InvalidOperationException(
                $"Shopee respondeu {(int)response.StatusCode}: {Truncate(body)}");
        }

        var parsed = JsonSerializer.Deserialize<ShopeeGraphQlResponse>(body, ShopeeJson.Options);

        if (parsed?.Errors is { Count: > 0 } errors)
        {
            var messages = string.Join(" | ", errors.Select(e => e.Message));
            throw new InvalidOperationException($"Shopee devolveu erro de GraphQL: {messages}");
        }

        return parsed?.Data?.ProductOfferV2;
    }

    private static string Truncate(string value) =>
        value.Length <= 500 ? value : value[..500];
}
