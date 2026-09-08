# 🏷️ Mega Descontão

Vitrine agregadora de promoções dos grandes marketplaces (Mercado Livre, Shopee, Temu, Amazon…).
O visitante garimpa as ofertas organizadas por loja, categoria e desconto e, ao clicar em
**“Aproveitar oferta”**, passa por `/api/go/{offerId}` — que registra o clique e redireciona para o
link de afiliado guardado no banco.

O Mega Descontão não processa pagamento e não tenta substituir o checkout das lojas. O valor está
em **garimpar, organizar e apresentar**.

## Stack

| Camada    | Tecnologia                                       |
| --------- | ------------------------------------------------ |
| Front-end | React 19 + Vite                                  |
| Back-end  | ASP.NET Core 8 (Minimal API)                     |
| Dados     | EF Core + SQLite (troca para PostgreSQL/SQL Server sem mexer no domínio) |

```
React  →  Minimal API  →  EF Core  →  SQLite
                ↑
        IMarketplaceProvider (Mercado Livre / Shopee / Temu / feed JSON)
```

O React não guarda regra de negócio: preço, status, link de afiliado e contagem de cliques vivem
no back-end. A busca e os filtros também são do servidor — o navegador nunca baixa o catálogo
inteiro para filtrar.

## Modelo de dados

```
Category 1─────n Product 1─────n ProductOffer n─────1 Store
                                      │
                                      ├──n PriceHistory
                                      └──n OfferClick
```

O mesmo produto pode ter uma oferta em cada marketplace — é o `ProductOffer` que carrega preço,
link de afiliado e situação:

```
Fone Bluetooth
 ├── Mercado Livre → R$ 189,90 → link afiliado ML
 └── Shopee        → R$ 179,90 → link afiliado Shopee   ← é esta que a vitrine mostra
```

| Tabela         | Papel |
| -------------- | ----- |
| `Categories`   | Nome + slug usado nas URLs e filtros |
| `Stores`       | Marketplace: nome, slug, logo |
| `Products`     | O produto em si + `SearchText` (busca sem acento) |
| `Offers`       | Preço, `AffiliateUrl`, `Status`, `ExpiresAt`, `LastCheckedAt`, `ClickCount`, `ExternalProductId` |
| `PriceHistory` | Cada preço observado, com data — base para “menor preço em 30 dias” |
| `Clicks`       | Um registro por clique, para relatório por período |

`Status` da oferta: `Active`, `Expired`, `Unavailable`, `Hidden`. Só `Active` (e dentro do prazo de
`ExpiresAt`) aparece na vitrine.

## Como rodar

Pré-requisitos: **.NET SDK 8** e **Node.js 20.19+**.

### 1. API

```bash
cd backend/MegaDescontao.Api
dotnet user-secrets set "Admin:ApiKey" "uma-chave-qualquer"   # opcional, libera /api/admin/*
dotnet run
```

Sobe em `http://localhost:5080`. Na primeira execução as migrations criam o `megadescontao.db` e
inserem o catálogo de demonstração. Swagger em `/swagger`.

### 2. Front-end

```bash
cd frontend
npm install
npm run dev
```

Abre em `http://localhost:5173`. A URL da API vem de `VITE_API_URL` (veja `.env.development`).

## Endpoints

### Público

| Método | Rota                    | Descrição |
| ------ | ----------------------- | --------- |
| `GET`  | `/api/products`         | Vitrine — `search`, `category`, `store`, `sort`, `minDiscount`, `page`, `pageSize` |
| `GET`  | `/api/products/{id}`    | Produto com todas as ofertas ativas + menor/média de preço em 30 dias |
| `GET`  | `/api/categories`       | Categorias que hoje têm oferta ativa |
| `GET`  | `/api/stores`           | Lojas que hoje têm oferta ativa |
| `GET`  | `/api/go/{offerId}`     | **Registra o clique e redireciona (302) para o link de afiliado** |
| `GET`  | `/health`               | Health check |

`sort`: `recentes` (padrão), `maior-desconto`, `menor-preco`, `maior-preco`, `populares`.
`category` e `store` recebem **slug** (`casa-e-cozinha`, `mercado-livre`).

### Administração

Todas exigem o cabeçalho `X-Admin-Key`. **Sem `Admin:ApiKey` configurada, as rotas respondem 503** —
esquecer de configurar desliga a administração em vez de deixá-la aberta.

| Método   | Rota |
| -------- | ---- |
| `GET`    | `/api/admin/products` (inclui inativos e ofertas fora do ar) |
| `POST`   | `/api/admin/products` |
| `PUT`    | `/api/admin/products/{id}` |
| `DELETE` | `/api/admin/products/{id}` |
| `POST`   | `/api/admin/products/{id}/offers` (mais um marketplace no mesmo produto) |
| `PUT`    | `/api/admin/offers/{offerId}` |
| `DELETE` | `/api/admin/offers/{offerId}` |
| `POST`   | `/api/admin/categories` · `/api/admin/stores` |
| `GET`    | `/api/admin/clicks` |
| `POST`   | `/api/admin/import/{storeSlug}` |
| `POST`   | `/api/admin/offers/expire` |

```bash
curl -X POST http://localhost:5080/api/admin/products \
  -H "X-Admin-Key: uma-chave-qualquer" -H "Content-Type: application/json" \
  -d '{
    "title": "Echo Dot 5ª geração",
    "imageUrl": "https://exemplo.com/echo.jpg",
    "categorySlug": "eletronicos",
    "offers": [{
      "storeSlug": "amazon",
      "currentPrice": 279.00,
      "originalPrice": 499.00,
      "affiliateUrl": "https://www.amazon.com.br/dp/XXXX?tag=SEU-TAG-20"
    }]
  }'
```

## Fluxo do clique

```
usuário clica "Aproveitar oferta"
   → navegador vai para GET /api/go/{offerId}      (só um id; nenhuma URL vem do front)
   → API confere: oferta Active, dentro do prazo, produto ativo
   → grava a linha em Clicks e faz UPDATE ClickCount = ClickCount + 1  (atômico)
   → responde 302 para a AffiliateUrl lida do banco
```

A `AffiliateUrl` **não existe em nenhuma resposta pública** da API. O front só conhece o `offerId`,
então não há como pular o contador nem adulterar o destino.

## Importação de marketplaces

```
IMarketplaceProvider  →  MarketplaceOffer (formato interno)  →  ProductImportService  →  banco
```

Cada loja implementa `IMarketplaceProvider` e traduz o formato dela (`title/price/image` no ML,
`name/current_price/image_url` na Shopee) para o `MarketplaceOffer`. Nada de formato de marketplace
vaza para o domínio, para os endpoints ou para o banco.

O `ProductImportService` é idempotente: a identidade da oferta é o par **(loja, id externo)**, então
rodar o mesmo feed duas vezes atualiza em vez de duplicar. Ele também grava `PriceHistory` a cada
mudança de preço e marca como `Unavailable` o que sumiu da origem — mas só quando o provider declara
`ProvidesFullSnapshot`, porque num feed parcial a ausência não significa nada.

**Estado atual dos providers:**

| Provider | Situação |
| -------- | -------- |
| `JsonFeedProvider` | ✅ Funcionando — importa de um arquivo JSON local |
| `MercadoLivreProvider` | ⏳ Preparado, aguarda credenciais (aplicação registrada + Programa de Afiliados) |
| `ShopeeProvider` | ⏳ Preparado, aguarda conta de afiliado aprovada + appId/secret |
| `TemuProvider` | ⏳ Slot criado |

Enquanto as APIs oficiais não são liberadas, o feed JSON alimenta o catálogo:

```jsonc
// appsettings.Development.json
"Import": {
  "JsonFeeds": [
    { "StoreSlug": "shopee", "DisplayName": "Shopee", "FilePath": "feeds/exemplo-shopee.json" }
  ]
}
```

```bash
curl -X POST http://localhost:5080/api/admin/import/shopee -H "X-Admin-Key: ..."
```

Quando as credenciais oficiais chegarem, o provider oficial passa a vencer automaticamente sobre o
feed manual (o importador prefere o provider configurado) — sem mudar código.

## Segurança

- A `AffiliateUrl` nunca sai na API pública; o redirect é endereçado por id inteiro, então não há
  `?url=` para manipular e nem como virar open redirect.
- Só `http` e `https` entram no banco, validado na escrita (admin **e** importação).
- Rotas administrativas exigem `X-Admin-Key` (comparação de tempo fixo) e ficam desligadas sem chave.
- Rate limit: 60 req/min por IP no `/api/go`, 30 req/min no `/api/admin`.
- Segredos por `user-secrets`/variáveis de ambiente — `appsettings.json` só tem campos vazios.
- CORS restrito às origens de `Cors:AllowedOrigins`.
- Erros saem como ProblemDetails; falha ao gravar clique é logada mas **não** impede o redirect.

## Estrutura

```
mega-descontao/
├── backend/MegaDescontao.Api/
│   ├── Common/        TextSearch (acentos, slug), UrlValidation
│   ├── Contracts/     DTOs público e administrativo
│   ├── Data/          AppDbContext, SeedData, Migrations
│   ├── Endpoints/     Product, Catalog, Go, Admin, RateLimitPolicies
│   ├── Marketplaces/  IMarketplaceProvider, ProductImportService, Providers/
│   ├── Models/        Category, Store, Product, ProductOffer, PriceHistory, OfferClick
│   ├── Security/      AdminApiKeyFilter
│   └── feeds/         Feeds JSON de importação manual
└── frontend/src/
    ├── components/    Header, FilterBar, ProductCard, ProductGrid, Pagination, Footer
    ├── hooks/         useProducts, useFilterOptions, useDebouncedValue
    ├── utils/         formatação de preço e cores das lojas
    └── api.js         cliente HTTP + montagem da URL de /api/go/{offerId}
```

## Próximos passos

1. **Credenciais oficiais**: registrar a aplicação no Mercado Livre e pedir a conta de afiliado da
   Shopee (aprovação leva dias) — é o que destrava os dois providers.
2. **Agendador**: hoje a importação é disparada por endpoint. Com um `BackgroundService` +
   `PeriodicTimer`, dá para rodar em cadência escalonada (ofertas quentes com mais frequência que o
   catálogo inteiro) e chamar `ExpireOutdatedOffersAsync` de tempos em tempos.
3. **Painel admin**: a API administrativa já existe; falta a tela.
4. **PromotionScore**: `PriceHistory` já acumula os dados; falta a regra que compara preço atual com
   a média histórica para destacar oferta boa de verdade.
5. **SEO**: hoje é SPA pura — o conteúdo só existe depois do JS rodar, o que limita indexação. Se
   busca orgânica virar canal importante, o caminho é SSR (Next.js) ou pré-renderização das páginas
   de produto, sem trocar o back-end.
