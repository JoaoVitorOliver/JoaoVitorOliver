using MegaDescontao.Api.Models;

namespace MegaDescontao.Api.Data;

/// Catálogo de demonstração para o MVP rodar de ponta a ponta já na primeira execução.
/// Os links apontam para páginas públicas de busca dos marketplaces — troque-os pelos
/// seus links de afiliado reais (ou cadastre novas ofertas via POST /api/offers).
public static class SeedData
{
    public static void EnsureSeeded(AppDbContext db)
    {
        if (db.Offers.Any())
        {
            return;
        }

        var now = DateTime.UtcNow;

        db.Offers.AddRange(
            new Offer
            {
                Title = "Fone de Ouvido Bluetooth com Cancelamento de Ruído",
                Description = "Até 40h de bateria, driver de 40mm e estojo de carregamento rápido.",
                ImageUrl = "https://picsum.photos/seed/fone-bluetooth/600/600",
                Price = 189.90m,
                OriginalPrice = 399.90m,
                Store = "Mercado Livre",
                Category = "Eletrônicos",
                AffiliateUrl = "https://lista.mercadolivre.com.br/fone-de-ouvido-bluetooth",
                ClickCount = 87,
                CreatedAt = now.AddDays(-1)
            },
            new Offer
            {
                Title = "Smartwatch com Monitor Cardíaco e GPS",
                Description = "Tela AMOLED 1.85\", resistente à água e mais de 100 modos esportivos.",
                ImageUrl = "https://picsum.photos/seed/smartwatch/600/600",
                Price = 149.00m,
                OriginalPrice = 349.00m,
                Store = "Shopee",
                Category = "Eletrônicos",
                AffiliateUrl = "https://shopee.com.br/search?keyword=smartwatch",
                ClickCount = 212,
                CreatedAt = now.AddDays(-2)
            },
            new Offer
            {
                Title = "Air Fryer Digital 5L Antiaderente",
                Description = "Painel touch, 8 programas prontos e cesto removível de fácil limpeza.",
                ImageUrl = "https://picsum.photos/seed/air-fryer/600/600",
                Price = 279.90m,
                OriginalPrice = 549.90m,
                Store = "Mercado Livre",
                Category = "Casa e Cozinha",
                AffiliateUrl = "https://lista.mercadolivre.com.br/air-fryer",
                ClickCount = 341,
                CreatedAt = now.AddDays(-3)
            },
            new Offer
            {
                Title = "Kit 4 Organizadores Dobráveis para Armário",
                Description = "Tecido reforçado com visor frontal — ideal para roupas e brinquedos.",
                ImageUrl = "https://picsum.photos/seed/organizador/600/600",
                Price = 39.90m,
                OriginalPrice = 99.90m,
                Store = "Temu",
                Category = "Casa e Cozinha",
                AffiliateUrl = "https://www.temu.com/search_result.html?search_key=organizador%20de%20armario",
                ClickCount = 64,
                CreatedAt = now.AddDays(-1)
            },
            new Offer
            {
                Title = "Teclado Mecânico Gamer RGB ABNT2",
                Description = "Switch blue, anti-ghosting e iluminação com 16 efeitos.",
                ImageUrl = "https://picsum.photos/seed/teclado-gamer/600/600",
                Price = 199.90m,
                OriginalPrice = 329.90m,
                Store = "Mercado Livre",
                Category = "Games",
                AffiliateUrl = "https://lista.mercadolivre.com.br/teclado-mecanico-gamer",
                ClickCount = 128,
                CreatedAt = now.AddDays(-4)
            },
            new Offer
            {
                Title = "Cadeira Gamer Reclinável com Apoio Lombar",
                Description = "Encosto reclinável até 180°, apoio de braço 2D e suporta 120kg.",
                ImageUrl = "https://picsum.photos/seed/cadeira-gamer/600/600",
                Price = 699.00m,
                OriginalPrice = 1299.00m,
                Store = "Shopee",
                Category = "Games",
                AffiliateUrl = "https://shopee.com.br/search?keyword=cadeira%20gamer",
                ClickCount = 95,
                CreatedAt = now.AddDays(-5)
            },
            new Offer
            {
                Title = "Tênis Esportivo Leve para Corrida",
                Description = "Solado em EVA com amortecimento e cabedal respirável em malha.",
                ImageUrl = "https://picsum.photos/seed/tenis-corrida/600/600",
                Price = 119.90m,
                OriginalPrice = 259.90m,
                Store = "Shopee",
                Category = "Moda e Acessórios",
                AffiliateUrl = "https://shopee.com.br/search?keyword=tenis%20de%20corrida",
                ClickCount = 176,
                CreatedAt = now.AddDays(-2)
            },
            new Offer
            {
                Title = "Mochila Antifurto para Notebook 15.6\"",
                Description = "Compartimento acolchoado, porta USB externa e tecido impermeável.",
                ImageUrl = "https://picsum.photos/seed/mochila-notebook/600/600",
                Price = 89.90m,
                OriginalPrice = 189.90m,
                Store = "Temu",
                Category = "Moda e Acessórios",
                AffiliateUrl = "https://www.temu.com/search_result.html?search_key=mochila%20notebook",
                ClickCount = 43,
                CreatedAt = now.AddDays(-6)
            },
            new Offer
            {
                Title = "Kit Skincare Facial com Vitamina C",
                Description = "Sérum, hidratante e protetor solar FPS 50 para uso diário.",
                ImageUrl = "https://picsum.photos/seed/skincare/600/600",
                Price = 74.90m,
                OriginalPrice = 159.90m,
                Store = "Shopee",
                Category = "Beleza",
                AffiliateUrl = "https://shopee.com.br/search?keyword=kit%20skincare",
                ClickCount = 152,
                CreatedAt = now.AddDays(-3)
            },
            new Offer
            {
                Title = "Secador de Cabelo Profissional 2000W",
                Description = "Íons negativos, 3 temperaturas e difusor incluso.",
                ImageUrl = "https://picsum.photos/seed/secador/600/600",
                Price = 129.90m,
                OriginalPrice = 249.90m,
                Store = "Mercado Livre",
                Category = "Beleza",
                AffiliateUrl = "https://lista.mercadolivre.com.br/secador-de-cabelo",
                ClickCount = 71,
                CreatedAt = now.AddDays(-7)
            },
            new Offer
            {
                Title = "Parafusadeira Furadeira 12V com 50 Acessórios",
                Description = "2 baterias de lítio, maleta rígida e torque ajustável em 18 níveis.",
                ImageUrl = "https://picsum.photos/seed/parafusadeira/600/600",
                Price = 219.90m,
                OriginalPrice = 429.90m,
                Store = "Mercado Livre",
                Category = "Ferramentas",
                AffiliateUrl = "https://lista.mercadolivre.com.br/parafusadeira-furadeira",
                ClickCount = 58,
                CreatedAt = now.AddDays(-4)
            },
            new Offer
            {
                Title = "Jogo de Chaves de Precisão 115 em 1",
                Description = "Pontas magnéticas em aço S2 para celulares, notebooks e consoles.",
                ImageUrl = "https://picsum.photos/seed/chaves-precisao/600/600",
                Price = 49.90m,
                OriginalPrice = 129.90m,
                Store = "Temu",
                Category = "Ferramentas",
                AffiliateUrl = "https://www.temu.com/search_result.html?search_key=kit%20chaves%20precisao",
                ClickCount = 39,
                CreatedAt = now.AddDays(-8)
            },
            new Offer
            {
                Title = "Luminária de Mesa LED com Carregador Wireless",
                Description = "3 temperaturas de luz, braço articulado e carregamento por indução.",
                ImageUrl = "https://picsum.photos/seed/luminaria-led/600/600",
                Price = 99.90m,
                OriginalPrice = 219.90m,
                Store = "Temu",
                Category = "Casa e Cozinha",
                AffiliateUrl = "https://www.temu.com/search_result.html?search_key=luminaria%20led%20mesa",
                ClickCount = 27,
                CreatedAt = now.AddDays(-2)
            },
            new Offer
            {
                Title = "Caixa de Som Bluetooth à Prova d'Água 20W",
                Description = "IPX7, até 24h de reprodução e pareamento estéreo entre duas unidades.",
                ImageUrl = "https://picsum.photos/seed/caixa-som/600/600",
                Price = 159.90m,
                OriginalPrice = 299.90m,
                Store = "Amazon",
                Category = "Eletrônicos",
                AffiliateUrl = "https://www.amazon.com.br/s?k=caixa+de+som+bluetooth",
                ClickCount = 113,
                CreatedAt = now.AddDays(-5)
            },
            new Offer
            {
                Title = "Cama Box para Pet com Almofada Removível",
                Description = "Tecido lavável e base antiderrapante — tamanhos P, M e G.",
                ImageUrl = "https://picsum.photos/seed/cama-pet/600/600",
                Price = 79.90m,
                OriginalPrice = 169.90m,
                Store = "Shopee",
                Category = "Pet",
                AffiliateUrl = "https://shopee.com.br/search?keyword=cama%20para%20pet",
                ClickCount = 66,
                CreatedAt = now.AddDays(-6)
            },
            new Offer
            {
                Title = "Kit 2 Garrafas Térmicas Inox 1L",
                Description = "Mantém a temperatura por até 24h, com alça e tampa antivazamento.",
                ImageUrl = "https://picsum.photos/seed/garrafa-termica/600/600",
                Price = 89.90m,
                OriginalPrice = 199.90m,
                Store = "Amazon",
                Category = "Esporte",
                AffiliateUrl = "https://www.amazon.com.br/s?k=garrafa+termica+inox",
                ClickCount = 34,
                CreatedAt = now.AddDays(-9)
            });

        db.SaveChanges();
    }
}
