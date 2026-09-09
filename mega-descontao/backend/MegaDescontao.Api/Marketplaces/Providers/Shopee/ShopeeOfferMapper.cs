namespace MegaDescontao.Api.Marketplaces.Providers.Shopee;

/// Traduz a resposta da Shopee para o formato interno. Fica separado do provider porque
/// é a parte que dá para testar sem rede — e é onde mora a regra chata do preço original.
public static class ShopeeOfferMapper
{
    /// Devolve null quando a oferta não serve para o catálogo (sem link, sem nome, sem preço).
    public static MarketplaceOffer? ToMarketplaceOffer(ShopeeOfferNode node, ShopeeOptions options)
    {
        if (string.IsNullOrWhiteSpace(node.OfferLink) || string.IsNullOrWhiteSpace(node.ProductName))
        {
            return null;
        }

        var currentPrice = node.PriceMin ?? node.PriceMax;
        if (currentPrice is null || currentPrice <= 0)
        {
            return null;
        }

        return new MarketplaceOffer(
            ExternalProductId: $"{node.ShopId}-{node.ItemId}",
            Title: node.ProductName.Trim(),
            // A API de afiliado não expõe descrição do produto. Melhor ficar sem do que
            // inventar texto ou copiar de outro lugar.
            Description: null,
            ImageUrl: node.ImageUrl?.Trim() ?? string.Empty,
            CurrentPrice: currentPrice.Value,
            OriginalPrice: DeriveOriginalPrice(currentPrice.Value, node.PriceDiscountRate),
            ProductUrl: string.IsNullOrWhiteSpace(node.ProductLink) ? node.OfferLink.Trim() : node.ProductLink.Trim(),
            AffiliateUrl: node.OfferLink.Trim(),
            CategoryName: ResolveCategory(node, options),
            ExpiresAt: ToDateTime(node.PeriodEndTime));
    }

    /// A Shopee manda o percentual de desconto, não o preço de antes. O "de" que aparece
    /// no card é reconstruído a partir dele, então é uma aproximação — pode divergir em
    /// centavos do que a loja mostra.
    public static decimal? DeriveOriginalPrice(decimal currentPrice, decimal? discountRate)
    {
        if (discountRate is null or <= 0 or >= 100)
        {
            return null;
        }

        var original = Math.Round(currentPrice / (1 - (discountRate.Value / 100m)), 2);

        return original > currentPrice ? original : null;
    }

    public static DateTime? ToDateTime(long unixSeconds) =>
        unixSeconds > 0 ? DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime : null;

    private static string ResolveCategory(ShopeeOfferNode node, ShopeeOptions options)
    {
        var categoryId = node.ProductCatIds?.FirstOrDefault(id => id > 0);

        if (categoryId is > 0 &&
            options.CategoryMap.TryGetValue(categoryId.Value.ToString(), out var mapped) &&
            !string.IsNullOrWhiteSpace(mapped))
        {
            return mapped;
        }

        return options.DefaultCategory;
    }
}
