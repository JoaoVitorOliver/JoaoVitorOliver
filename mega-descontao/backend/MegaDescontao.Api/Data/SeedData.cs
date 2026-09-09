using MegaDescontao.Api.Common;
using MegaDescontao.Api.Models;

namespace MegaDescontao.Api.Data;

/// Catálogo de demonstração para o MVP rodar de ponta a ponta na primeira execução.
/// Os links apontam para páginas públicas de busca dos marketplaces: troque pelos seus
/// links de afiliado reais (via /api/admin/products) antes de publicar.
public static class SeedData
{
    private sealed record SeedOffer(
        string StoreSlug,
        decimal Price,
        decimal? OriginalPrice,
        string AffiliateUrl,
        int Clicks,
        int? ExpiresInHours = null);

    private sealed record SeedProduct(
        string Title,
        string Description,
        string ImageSeed,
        string CategorySlug,
        int DaysAgo,
        SeedOffer[] Offers);

    private static readonly (string Name, string Slug)[] Categories =
    [
        ("Eletrônicos", "eletronicos"),
        ("Casa e Cozinha", "casa-e-cozinha"),
        ("Moda e Acessórios", "moda-e-acessorios"),
        ("Beleza", "beleza"),
        ("Games", "games"),
        ("Ferramentas", "ferramentas"),
        ("Pet", "pet"),
        ("Esporte", "esporte"),
    ];

    private static readonly (string Name, string Slug)[] Stores =
    [
        ("Mercado Livre", "mercado-livre"),
        ("Shopee", "shopee"),
    ];

    private static readonly SeedProduct[] Products =
    [
        new("Fone de Ouvido Bluetooth com Cancelamento de Ruído",
            "Até 40h de bateria, driver de 40mm e estojo de carregamento rápido.",
            "fone-bluetooth", "eletronicos", 1,
            [
                new("mercado-livre", 189.90m, 399.90m, "https://lista.mercadolivre.com.br/fone-de-ouvido-bluetooth", 88),
                // O mesmo produto em duas lojas: é para isso que a oferta vive separada do produto.
                new("shopee", 179.90m, 399.90m, "https://shopee.com.br/search?keyword=fone%20bluetooth", 54),
            ]),
        new("Smartwatch com Monitor Cardíaco e GPS",
            "Tela AMOLED 1.85\", resistente à água e mais de 100 modos esportivos.",
            "smartwatch", "eletronicos", 2,
            // Promoção relâmpago: sai da vitrine sozinha quando o prazo vence.
            [new("shopee", 149.00m, 349.00m, "https://shopee.com.br/search?keyword=smartwatch", 212, ExpiresInHours: 6)]),
        new("Air Fryer Digital 5L Antiaderente",
            "Painel touch, 8 programas prontos e cesto removível de fácil limpeza.",
            "air-fryer", "casa-e-cozinha", 3,
            [new("mercado-livre", 279.90m, 549.90m, "https://lista.mercadolivre.com.br/air-fryer", 341)]),
        new("Kit 4 Organizadores Dobráveis para Armário",
            "Tecido reforçado com visor frontal — ideal para roupas e brinquedos.",
            "organizador", "casa-e-cozinha", 1,
            [new("shopee", 39.90m, 99.90m, "https://shopee.com.br/search?keyword=organizador%20de%20armario", 65)]),
        new("Teclado Mecânico Gamer RGB ABNT2",
            "Switch blue, anti-ghosting e iluminação com 16 efeitos.",
            "teclado-gamer", "games", 4,
            [new("mercado-livre", 199.90m, 329.90m, "https://lista.mercadolivre.com.br/teclado-mecanico-gamer", 128)]),
        new("Cadeira Gamer Reclinável com Apoio Lombar",
            "Encosto reclinável até 180°, apoio de braço 2D e suporta 120kg.",
            "cadeira-gamer", "games", 5,
            [new("shopee", 699.00m, 1299.00m, "https://shopee.com.br/search?keyword=cadeira%20gamer", 95)]),
        new("Tênis Esportivo Leve para Corrida",
            "Solado em EVA com amortecimento e cabedal respirável em malha.",
            "tenis-corrida", "moda-e-acessorios", 2,
            [new("shopee", 119.90m, 259.90m, "https://shopee.com.br/search?keyword=tenis%20de%20corrida", 176)]),
        new("Mochila Antifurto para Notebook 15.6\"",
            "Compartimento acolchoado, porta USB externa e tecido impermeável.",
            "mochila-notebook", "moda-e-acessorios", 6,
            [new("shopee", 89.90m, 189.90m, "https://shopee.com.br/search?keyword=mochila%20notebook", 43)]),
        new("Kit Skincare Facial com Vitamina C",
            "Sérum, hidratante e protetor solar FPS 50 para uso diário.",
            "skincare", "beleza", 3,
            [new("shopee", 74.90m, 159.90m, "https://shopee.com.br/search?keyword=kit%20skincare", 152)]),
        new("Secador de Cabelo Profissional 2000W",
            "Íons negativos, 3 temperaturas e difusor incluso.",
            "secador", "beleza", 7,
            [new("mercado-livre", 129.90m, 249.90m, "https://lista.mercadolivre.com.br/secador-de-cabelo", 71)]),
        new("Parafusadeira Furadeira 12V com 50 Acessórios",
            "2 baterias de lítio, maleta rígida e torque ajustável em 18 níveis.",
            "parafusadeira", "ferramentas", 4,
            [new("mercado-livre", 219.90m, 429.90m, "https://lista.mercadolivre.com.br/parafusadeira-furadeira", 58)]),
        new("Jogo de Chaves de Precisão 115 em 1",
            "Pontas magnéticas em aço S2 para celulares, notebooks e consoles.",
            "chaves-precisao", "ferramentas", 8,
            [new("mercado-livre", 49.90m, 129.90m, "https://lista.mercadolivre.com.br/kit-chaves-precisao", 39)]),
        new("Luminária de Mesa LED com Carregador Wireless",
            "3 temperaturas de luz, braço articulado e carregamento por indução.",
            "luminaria-led", "casa-e-cozinha", 2,
            [new("shopee", 99.90m, 219.90m, "https://shopee.com.br/search?keyword=luminaria%20led%20mesa", 27)]),
        new("Caixa de Som Bluetooth à Prova d'Água 20W",
            "IPX7, até 24h de reprodução e pareamento estéreo entre duas unidades.",
            "caixa-som", "eletronicos", 5,
            [new("mercado-livre", 159.90m, 299.90m, "https://lista.mercadolivre.com.br/caixa-de-som-bluetooth", 113)]),
        new("Cama Box para Pet com Almofada Removível",
            "Tecido lavável e base antiderrapante — tamanhos P, M e G.",
            "cama-pet", "pet", 6,
            [new("shopee", 79.90m, 169.90m, "https://shopee.com.br/search?keyword=cama%20para%20pet", 66)]),
        new("Kit 2 Garrafas Térmicas Inox 1L",
            "Mantém a temperatura por até 24h, com alça e tampa antivazamento.",
            "garrafa-termica", "esporte", 9,
            [new("mercado-livre", 89.90m, 199.90m, "https://lista.mercadolivre.com.br/garrafa-termica-inox", 34)]),
    ];

    public static void EnsureSeeded(AppDbContext db)
    {
        if (db.Products.Any())
        {
            return;
        }

        var now = DateTime.UtcNow;

        var categories = Categories.ToDictionary(
            c => c.Slug,
            c => new Category { Name = c.Name, Slug = c.Slug });

        var stores = Stores.ToDictionary(
            s => s.Slug,
            s => new Store { Name = s.Name, Slug = s.Slug });

        db.Categories.AddRange(categories.Values);
        db.Stores.AddRange(stores.Values);

        foreach (var seed in Products)
        {
            var createdAt = now.AddDays(-seed.DaysAgo);

            var product = new Product
            {
                Title = seed.Title,
                Description = seed.Description,
                ImageUrl = $"https://picsum.photos/seed/{seed.ImageSeed}/600/600",
                Category = categories[seed.CategorySlug],
                SearchText = TextSearch.Normalize(seed.Title, seed.Description),
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
            };

            foreach (var seedOffer in seed.Offers)
            {
                var offer = new ProductOffer
                {
                    Store = stores[seedOffer.StoreSlug],
                    CurrentPrice = seedOffer.Price,
                    OriginalPrice = seedOffer.OriginalPrice,
                    AffiliateUrl = seedOffer.AffiliateUrl,
                    Status = OfferStatus.Active,
                    ClickCount = seedOffer.Clicks,
                    ExpiresAt = seedOffer.ExpiresInHours is int hours ? now.AddHours(hours) : null,
                    LastCheckedAt = now,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt,
                };

                // Alguns pontos de histórico para que "menor preço em 30 dias" já tenha o que responder.
                offer.PriceHistory.AddRange(BuildPriceHistory(seedOffer, now));

                product.Offers.Add(offer);
            }

            db.Products.Add(product);
        }

        db.SaveChanges();
    }

    private static IEnumerable<PriceHistory> BuildPriceHistory(SeedOffer offer, DateTime now)
    {
        var reference = offer.OriginalPrice ?? offer.Price;
        decimal[] steps = [reference, Math.Round(reference * 0.85m, 2), Math.Round(offer.Price * 1.1m, 2), offer.Price];

        for (var i = 0; i < steps.Length; i++)
        {
            yield return new PriceHistory
            {
                Price = steps[i],
                CollectedAt = now.AddDays(-28 + (i * 9)),
            };
        }
    }
}
