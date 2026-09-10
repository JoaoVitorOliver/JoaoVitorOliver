# 🏷️ Mega Descontão

Vitrine agregadora de promoções de marketplaces. Hoje o foco é **Mercado Livre e Shopee**; a
arquitetura aceita outras lojas sem mudança estrutural.
O visitante garimpa as ofertas organizadas por loja, categoria e desconto e, ao clicar em
**“Aproveitar oferta”**, passa por `/api/go/{offerId}` — que registra o clique e redireciona para o
link de afiliado guardado no banco.

O Mega Descontão não processa pagamento e não tenta substituir o checkout das lojas. O valor está
em **garimpar, organizar e apresentar**.

## Stack

| Camada    | Tecnologia                                       |
| --------- | ------------------------------------------------ |
| Front-end | React 19 + Vite                                  |
| Back-end  | ASP.NET Core 10 (Minimal API)                    |
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

Pré-requisitos: **.NET SDK 10** e **Node.js 20.19+**.

No Windows, o atalho para subir as duas pontas de uma vez:

```powershell
.\dev.ps1
```

Ele instala as dependências do front na primeira execução e abre uma janela para cada processo.
Se o PowerShell bloquear a execução do script, use `powershell -ExecutionPolicy Bypass -File .\dev.ps1`.

Para rodar cada parte na mão:

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

### Se a API subir e ficar parada

Se o `dotnet run` imprimir `Acquiring an exclusive lock for migration application` e nunca abrir a
porta, é a trava de migration do EF Core: ela é gravada no banco antes de aplicar migrations e some
no fim, mas fica presa se o processo morrer no meio. O código só chama `Migrate()` quando há
migration pendente, o que evita o caso comum — mas se acontecer com uma migration realmente
pendente, limpe a trava:

```sql
DELETE FROM __EFMigrationsLock;
```

Em desenvolvimento dá para simplesmente apagar `megadescontao.db*` — o banco é recriado com o
catálogo de demonstração no próximo start.

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
| `ShopeeProvider` | ✅ Implementado (GraphQL assinado) — só falta a credencial para ligar |
| `MercadoLivreProvider` | ⏳ Preparado, aguarda credenciais (aplicação registrada + Programa de Afiliados) |

### Caminho rápido: CSV de links em massa da Shopee

Não depende da Open API. Na Plataforma de Afiliados (web): **Oferta de Produto → selecione os
produtos → Obter Link**. A Shopee baixa um CSV com a coluna `Offer Link`. Suba esse arquivo:

```bash
curl.exe -X POST "http://localhost:5080/api/admin/import/shopee/csv?category=Beleza" \
  -H "X-Admin-Key: <sua-chave>" -F "file=@BatchProductLinks.csv"
```

O `category` é opcional e classifica o lote inteiro; sem ele, cai em `DefaultCategory`.

O importador é o mesmo da API: idempotente por (loja, id externo), com histórico de preço. E o id
externo sai como `shopId-itemId`, **o mesmo formato que o provider da Open API produz** — então,
quando a API for liberada, ela atualiza estas ofertas em vez de duplicá-las.

O que o CSV **não** traz, e o código não inventa: **foto**, **preço original** (logo, sem selo de
desconto) e **categoria por produto**. Enquanto isso, o card usa o placeholder no lugar da imagem.
Dá para completar pelo `PUT /api/admin/products/{id}` ou esperar a Open API, que traz imagem e
percentual de desconto.

### Ligando a Shopee

A Affiliate Open API devolve a oferta **com o link de afiliado já rastreado**, então importar
resolve dado e monetização na mesma chamada. Com as credenciais em mãos:

```bash
dotnet user-secrets set "Marketplaces:Shopee:ClientId" "<appId>"
dotnet user-secrets set "Marketplaces:Shopee:ClientSecret" "<secret>"
curl -X POST http://localhost:5080/api/admin/import/shopee -H "X-Admin-Key: <sua-chave>"
```

Ajustes disponíveis em `Marketplaces:Shopee`: `MaxOffers` (padrão 50), `PageSize`,
`DelayBetweenPagesMs` (a Shopee não publica o limite de requisições — a pausa entre páginas é
proposital até medirmos o limite real), `DefaultCategory` e `CategoryMap`, que traduz o id numérico
de categoria da Shopee para o nome usado no site.

Duas coisas que a API **não** entrega e o código contorna: não há descrição de produto (o campo fica
vazio em vez de inventar texto), e não há "preço de antes" — só o percentual de desconto, a partir do
qual o preço original é reconstruído. Por isso ele é uma aproximação e pode divergir em centavos do
que a loja mostra.

A assinatura das chamadas e a tradução dos dados estão cobertas por testes
(`backend/MegaDescontao.Tests`, `dotnet test`), porque são as duas partes que quebram calado — e as
únicas que dá para verificar sem as credenciais.

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

## Publicar da sua máquina (Cloudflare Tunnel)

Caminho sem custo e sem trocar o banco. Um terminal sobe o site, outro cria o túnel.

```powershell
# 1. sobe em modo produção, com a vitrine servida pela própria API
.\publish.ps1 -AdminKey "uma-chave-forte-de-verdade"

# 2. em outro terminal, expõe na internet com HTTPS
winget install --id Cloudflare.cloudflared
cloudflared tunnel --url http://localhost:8080
```

O `cloudflared` imprime uma URL `https://algo.trycloudflare.com` — é o endereço público do site,
sem precisar de conta nem domínio. A URL **muda a cada execução**: serve para testar e mostrar para
alguém, não para divulgar. Endereço fixo exige um domínio na Cloudflare e um túnel nomeado.

Três coisas que o script resolve e que quebrariam se você subisse na mão:

- **O banco fica em `data/`, fora de `publish/`.** A pasta de publicação é apagada e recriada a cada
  execução; com o banco dentro dela, cada republicação levaria junto o catálogo e o histórico.
- **`Hosting:BehindProxy` ligado.** Atrás do túnel, toda requisição chega de `127.0.0.1`. Sem ler o
  cabeçalho encaminhado, o rate limit por IP colocaria todos os visitantes na mesma cota de
  60 req/min e o site cairia com pouco movimento.
- **A chave de admin vai por variável de ambiente.** Em `Production` o `user-secrets` não é
  carregado — a chave que você configurou para o desenvolvimento simplesmente não vale aqui.

A aplicação escuta só em `localhost`: quem a alcança é o túnel, nunca a internet direto. Por isso
confiar no cabeçalho encaminhado é seguro nesse arranjo.

## Deploy em container

A imagem Docker empacota **tudo num processo só**: a API serve a API e a vitrine na mesma origem,
o que dispensa CORS e deixa um endereço só para configurar.

```bash
docker build -t mega-descontao .
docker run -p 8080:8080 \
  -v mega-dados:/data \
  -e Admin__ApiKey="uma-chave-forte-de-verdade" \
  mega-descontao
```

O que **precisa** estar certo em produção:

| Item | Por quê |
| ---- | ------- |
| `-v` num volume | O SQLite vive em `/data`. Sem volume, cada deploy apaga catálogo e histórico de cliques. |
| `Admin__ApiKey` | Sem ela, `/api/admin/*` responde 503 e você não consegue importar. Com uma fraca, qualquer um edita seu catálogo. |
| HTTPS | Fica a cargo do host (Fly, Railway, Azure, Render fazem isso sozinhos). |

Em produção o **seed não roda**: o site sobe vazio e é alimentado pelas ofertas reais. Publicar 16
produtos fictícios apontando para páginas de busca seria pior que não ter produto nenhum.

## Contas

Conta é **opcional**: o visitante anônimo usa o site inteiro. O botão **Entrar**, no canto direito
do header, abre o diálogo de login e cadastro.

Autenticação pelo **ASP.NET Core Identity**, com os endpoints prontos em `/api/auth`
(`register`, `login`, `me`, `logout`). Sessão por **cookie httpOnly** — o JavaScript não lê o cookie,
então nem XSS nem extensão conseguem roubá-lo, diferente de token no `localStorage`.

| Papel | O que muda |
| ----- | ---------- |
| Anônimo | Vê a vitrine completa |
| Usuário | Mesmo acesso, com conta própria (nichos e feed personalizado vêm na próxima etapa) |
| Admin | Menu ganha **Curadoria**, e `/api/admin/*` abre pela sessão, sem chave |

### Administrador

Em **produção**, o administrador é criado a partir da configuração — sem isso, nenhum é criado:

```bash
Admin__Email="voce@seudominio.com"
Admin__Password="uma-senha-forte"
```

Em **desenvolvimento**, se nada estiver configurado, sobe um par conhecido e o log avisa:
`admin@megadescontao.local` / `Admin@12345`. Ele existe só fora de produção.

A chave `Admin:ApiKey` continua valendo para automação (upload de CSV por `curl`, scripts). São dois
caminhos para a mesma porta: sessão para a tela, chave para máquina.

## Curadoria (`#/admin`)

O CSV de afiliado não traz **foto** nem **preço de antes**, e um card sem esses dois vira link, não
vitrine. A tela em `http://localhost:5173/#/admin` (ou `/#/admin` no site publicado) lista o que está
incompleto e deixa preencher os dois campos em lote. A chave administrativa fica em `sessionStorage`
— some quando a aba fecha.

Duas regras que sustentam isso:

- **Importação não apaga curadoria.** Campo que a origem não traz é preservado; campo que ela traz,
  ela atualiza. Sem essa guarda, a primeira reimportação varreria todo o trabalho manual. Coberto por
  teste (`ProductImportServiceTests`).
- **Sem preço de antes, nada de desconto inventado.** No lugar, o card mostra **“menor preço 30d”**
  quando o histórico coletado prova a afirmação: ao menos duas observações no período, a oferta já
  esteve mais cara e o preço de hoje é o menor. É um selo que dá para defender.

Quando a Open API for liberada, ela passa a trazer `imageUrl` e `priceDiscountRate` sozinha e a
curadoria manual deixa de ser necessária.

## Robô de expiração

Um `BackgroundService` sobe junto com a API e, a cada poucos minutos, marca como `Expired` toda
oferta ativa cujo `ExpiresAt` já passou. O intervalo fica em `Offers:ExpirationCheckMinutes`
(padrão 5 minutos, mínimo 1). Ele também roda uma passada no start, para o caso de o site ter ficado
parado enquanto promoções venciam.

A vitrine **já ignora** oferta vencida na hora da consulta, então uma promoção morta nunca aparece
para o visitante, mesmo entre dois ciclos do robô. O papel dele é deixar o banco honesto: sem isso, o
`Status` gravado mentiria e a tela do admin mostraria como ativa uma promoção que já acabou.

Falha no robô é logada e não derruba a API — desde o .NET 6, exceção não tratada em
`BackgroundService` encerra a aplicação inteira, e a vitrine não pode cair por causa de uma rotina
de manutenção.

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
2. **Importação agendada**: a expiração já roda sozinha; a *coleta* ainda é disparada por endpoint.
   O mesmo padrão do `OfferExpirationService` serve para ela, de preferência em cadência escalonada
   (ofertas relâmpago com mais frequência que o catálogo inteiro, para não estourar rate limit).
3. **Painel admin**: a API administrativa já existe; falta a tela.
4. **PromotionScore**: `PriceHistory` já acumula os dados; falta a regra que compara preço atual com
   a média histórica para destacar oferta boa de verdade.
5. **SEO**: hoje é SPA pura — o conteúdo só existe depois do JS rodar, o que limita indexação. Se
   busca orgânica virar canal importante, o caminho é SSR (Next.js) ou pré-renderização das páginas
   de produto, sem trocar o back-end.
