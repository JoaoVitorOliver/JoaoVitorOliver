using System.Text.Json;

namespace MegaDescontao.Api.Marketplaces.Providers;

public class JsonFeedOptions
{
    public string StoreSlug { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    /// Marque como true só se o arquivo representar o catálogo inteiro da loja.
    public bool FullSnapshot { get; set; }
}

/// Importa ofertas de um arquivo JSON local. Serve para alimentar o catálogo com
/// curadoria manual enquanto as APIs oficiais de afiliado não estão liberadas — e é
/// o caminho que deixa toda a esteira de importação testável desde já.
public class JsonFeedProvider(JsonFeedOptions options, ILogger<JsonFeedProvider> logger) : IMarketplaceProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public string StoreSlug => options.StoreSlug;

    public string DisplayName => string.IsNullOrWhiteSpace(options.DisplayName) ? options.StoreSlug : options.DisplayName;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.FilePath) && File.Exists(options.FilePath);

    public bool ProvidesFullSnapshot => options.FullSnapshot;

    public async Task<IReadOnlyList<MarketplaceOffer>> FetchOffersAsync(CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(options.FilePath);

        var offers = await JsonSerializer.DeserializeAsync<List<MarketplaceOffer>>(
            stream, SerializerOptions, cancellationToken);

        logger.LogInformation("Feed {Store}: {Count} ofertas lidas de {Path}",
            StoreSlug, offers?.Count ?? 0, options.FilePath);

        return offers ?? [];
    }
}
