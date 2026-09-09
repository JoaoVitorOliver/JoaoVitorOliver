using MegaDescontao.Api.Common;
using MegaDescontao.Api.Data;
using MegaDescontao.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MegaDescontao.Api.Marketplaces;

public record ImportResult(
    string StoreSlug,
    int Received,
    int Created,
    int Updated,
    int PriceChanges,
    int MarkedUnavailable,
    IReadOnlyList<string> Warnings);

/// Ponto único onde uma oferta de marketplace vira linha no banco. É idempotente:
/// rodar duas vezes o mesmo feed atualiza, não duplica, porque a identidade da oferta
/// é o par (loja, id externo).
public class ProductImportService(AppDbContext db, ILogger<ProductImportService> logger)
{
    public async Task<ImportResult> ImportAsync(IMarketplaceProvider provider, CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();

        if (!provider.IsConfigured)
        {
            warnings.Add($"O provider {provider.DisplayName} ainda não está configurado (faltam credenciais).");
            return new ImportResult(provider.StoreSlug, 0, 0, 0, 0, 0, warnings);
        }

        IReadOnlyList<MarketplaceOffer> offers;

        try
        {
            offers = await provider.FetchOffersAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A mensagem da loja (assinatura inválida, sem permissão de API, limite estourado)
            // é o diagnóstico. Devolver 500 sem corpo transformaria isso em adivinhação.
            logger.LogError(ex, "Falha ao buscar ofertas em {Store}", provider.StoreSlug);
            warnings.Add($"A chamada ao {provider.DisplayName} falhou: {ex.Message}");

            return new ImportResult(provider.StoreSlug, 0, 0, 0, 0, 0, warnings);
        }

        var now = DateTime.UtcNow;

        var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == provider.StoreSlug, cancellationToken);
        if (store is null)
        {
            store = new Store { Name = provider.DisplayName, Slug = provider.StoreSlug };
            db.Stores.Add(store);
            await db.SaveChangesAsync(cancellationToken);
        }

        var existing = await db.Offers
            .Include(o => o.Product)
            .Where(o => o.StoreId == store.Id && o.ExternalProductId != null)
            .ToDictionaryAsync(o => o.ExternalProductId!, cancellationToken);

        var categories = await db.Categories.ToDictionaryAsync(c => c.Slug, cancellationToken);

        int created = 0, updated = 0, priceChanges = 0;
        var seen = new HashSet<string>();

        foreach (var incoming in offers)
        {
            if (!IsUsable(incoming, out var reason))
            {
                warnings.Add($"Oferta {incoming.ExternalProductId} ignorada: {reason}");
                continue;
            }

            seen.Add(incoming.ExternalProductId);

            var categorySlug = TextSearch.Slugify(incoming.CategoryName);
            if (!categories.TryGetValue(categorySlug, out var category))
            {
                category = new Category { Name = incoming.CategoryName.Trim(), Slug = categorySlug };
                db.Categories.Add(category);
                categories[categorySlug] = category;
            }

            if (existing.TryGetValue(incoming.ExternalProductId, out var offer))
            {
                if (offer.CurrentPrice != incoming.CurrentPrice)
                {
                    db.PriceHistory.Add(new PriceHistory
                    {
                        ProductOffer = offer,
                        Price = incoming.CurrentPrice,
                        CollectedAt = now,
                    });
                    priceChanges++;
                }

                offer.CurrentPrice = incoming.CurrentPrice;
                offer.OriginalPrice = incoming.OriginalPrice;
                offer.AffiliateUrl = incoming.AffiliateUrl;
                offer.ProductUrl = incoming.ProductUrl;
                offer.ExpiresAt = incoming.ExpiresAt;
                offer.Status = OfferStatus.Active;
                offer.LastCheckedAt = now;
                offer.UpdatedAt = now;

                if (offer.Product is not null)
                {
                    offer.Product.Title = incoming.Title.Trim();
                    offer.Product.Description = incoming.Description?.Trim();
                    offer.Product.ImageUrl = incoming.ImageUrl;
                    offer.Product.Category = category;
                    offer.Product.SearchText = TextSearch.Normalize(incoming.Title, incoming.Description);
                    offer.Product.UpdatedAt = now;
                }

                updated++;
                continue;
            }

            var product = new Product
            {
                Title = incoming.Title.Trim(),
                Description = incoming.Description?.Trim(),
                ImageUrl = incoming.ImageUrl,
                Category = category,
                SearchText = TextSearch.Normalize(incoming.Title, incoming.Description),
                CreatedAt = now,
                UpdatedAt = now,
            };

            var newOffer = new ProductOffer
            {
                Product = product,
                Store = store,
                ExternalProductId = incoming.ExternalProductId,
                AffiliateUrl = incoming.AffiliateUrl,
                ProductUrl = incoming.ProductUrl,
                CurrentPrice = incoming.CurrentPrice,
                OriginalPrice = incoming.OriginalPrice,
                ExpiresAt = incoming.ExpiresAt,
                Status = OfferStatus.Active,
                LastCheckedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            };

            newOffer.PriceHistory.Add(new PriceHistory { Price = incoming.CurrentPrice, CollectedAt = now });

            db.Products.Add(product);
            db.Offers.Add(newOffer);
            created++;
        }

        var markedUnavailable = 0;

        if (provider.ProvidesFullSnapshot)
        {
            foreach (var (externalId, offer) in existing)
            {
                if (seen.Contains(externalId) || offer.Status != OfferStatus.Active)
                {
                    continue;
                }

                offer.Status = OfferStatus.Unavailable;
                offer.LastCheckedAt = now;
                offer.UpdatedAt = now;
                markedUnavailable++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Importação {Store}: {Received} recebidas, {Created} novas, {Updated} atualizadas, {PriceChanges} mudanças de preço, {Unavailable} fora do ar",
            provider.StoreSlug, offers.Count, created, updated, priceChanges, markedUnavailable);

        return new ImportResult(provider.StoreSlug, offers.Count, created, updated, priceChanges, markedUnavailable, warnings);
    }

    /// Expira as promoções cujo prazo passou. Fica aqui porque é a mesma responsabilidade
    /// de manter o catálogo honesto — quando existir agendador, ele chama este método.
    public async Task<int> ExpireOutdatedOffersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await db.Offers
            .Where(o => o.Status == OfferStatus.Active && o.ExpiresAt != null && o.ExpiresAt <= now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(o => o.Status, OfferStatus.Expired)
                    .SetProperty(o => o.UpdatedAt, now),
                cancellationToken);
    }

    private static bool IsUsable(MarketplaceOffer offer, out string reason)
    {
        if (string.IsNullOrWhiteSpace(offer.ExternalProductId))
        {
            reason = "sem id externo, não dá para deduplicar";
            return false;
        }

        if (string.IsNullOrWhiteSpace(offer.Title))
        {
            reason = "sem título";
            return false;
        }

        if (offer.CurrentPrice <= 0)
        {
            reason = "preço inválido";
            return false;
        }

        if (!UrlValidation.IsHttpUrl(offer.AffiliateUrl))
        {
            reason = "link de afiliado inválido";
            return false;
        }

        reason = string.Empty;
        return true;
    }
}
