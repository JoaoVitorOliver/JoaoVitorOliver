# 🏷️ Mega Descontão

Vitrine agregadora de promoções dos grandes marketplaces (Mercado Livre, Shopee, Temu, Amazon…).
O visitante navega pelas ofertas organizadas por loja, categoria e desconto e, ao clicar em
**“Aproveitar oferta”**, passa por `/api/go/{id}` — que registra o clique e redireciona para o
link de afiliado guardado no banco.

Este é o **MVP**: simples, funcionando de ponta a ponta, sem automação de coleta e sem painel admin.

## Stack

| Camada   | Tecnologia                                        |
| -------- | ------------------------------------------------- |
| Back-end | ASP.NET Core 8 (Minimal API) + EF Core + SQLite   |
| Front-end| React 19 + Vite                                    |

## Estrutura

```
mega-descontao/
├── backend/
│   ├── MegaDescontao.sln
│   └── MegaDescontao.Api/
│       ├── Contracts/      # DTOs de entrada e saída da API
│       ├── Data/           # DbContext, migrations e catálogo de demonstração
│       ├── Endpoints/      # Minimal API: ofertas e redirecionamento
│       ├── Models/         # Offer e OfferClick
│       └── Program.cs      # Composição: DbContext, CORS, Swagger, rotas
└── frontend/
    └── src/
        ├── components/     # Header, FilterBar, OfferCard, OfferGrid, Pagination, Footer
        ├── hooks/          # Busca de ofertas, filtros e debounce
        ├── utils/          # Formatação de preço e cores das lojas
        └── api.js          # Cliente HTTP e montagem da URL de /api/go/{id}
```

## Como rodar

Pré-requisitos: **.NET SDK 8.0+** e **Node.js 20+**.

### 1. API

```bash
cd backend/MegaDescontao.Api
dotnet run
```

Sobe em `http://localhost:5080`. Na primeira execução as migrations criam o `megadescontao.db`
e o catálogo de demonstração (16 ofertas) é inserido automaticamente.

Swagger disponível em `http://localhost:5080/swagger`.

### 2. Front-end

```bash
cd frontend
npm install
npm run dev
```

Abre em `http://localhost:5173`. A URL da API vem de `VITE_API_URL` (veja `.env.development`).

## Endpoints

| Método   | Rota                       | Descrição                                                        |
| -------- | -------------------------- | ---------------------------------------------------------------- |
| `GET`    | `/api/offers`              | Lista ofertas ativas — `search`, `store`, `category`, `sort`, `page`, `pageSize` |
| `GET`    | `/api/offers/filters`      | Lojas e categorias existentes, para montar os filtros da vitrine  |
| `GET`    | `/api/offers/{id}`         | Detalhe de uma oferta                                             |
| `POST`   | `/api/offers`              | Cadastra uma oferta                                               |
| `PUT`    | `/api/offers/{id}`         | Atualiza uma oferta                                               |
| `DELETE` | `/api/offers/{id}`         | Remove a oferta e o histórico de cliques dela                     |
| `GET`    | `/api/go/{id}`             | **Registra o clique e redireciona (302) para o link de afiliado** |
| `GET`    | `/health`                  | Health check                                                      |

Valores aceitos em `sort`: `recentes` (padrão), `maior-desconto`, `menor-preco`, `maior-preco`, `populares`.

### Cadastrando uma oferta

Enquanto não existe painel admin, o catálogo é alimentado pelo Swagger ou por HTTP:

```bash
curl -X POST http://localhost:5080/api/offers \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Echo Dot 5ª geração",
    "description": "Smart speaker com Alexa",
    "imageUrl": "https://exemplo.com/echo-dot.jpg",
    "price": 279.00,
    "originalPrice": 499.00,
    "store": "Amazon",
    "category": "Eletrônicos",
    "affiliateUrl": "https://www.amazon.com.br/dp/XXXX?tag=SEU-TAG-20",
    "isActive": true
  }'
```

## Decisões do MVP

- **A `affiliateUrl` nunca aparece na API pública.** A vitrine só conhece `/api/go/{id}`, então
  todo acesso à loja passa pelo contador de cliques.
- **Cliques gravados em duas frentes:** uma linha em `Clicks` (histórico para relatórios futuros)
  e o contador `ClickCount` na oferta, usado na ordenação por popularidade.
- **O redirect não quebra se a gravação falhar.** Perder a estatística custa menos do que perder
  a venda, então o usuário segue para a loja mesmo assim (a falha vai para o log).
- **Links são validados na entrada:** só `http://` e `https://` são aceitos, o que impede que o
  redirecionamento vire um vetor de ataque.
- **Preços convertidos para `double` no SQLite,** porque `decimal` é gravado como TEXT e quebraria
  a ordenação por preço.
- **Ofertas do seed apontam para páginas públicas de busca dos marketplaces** — troque pelos seus
  links de afiliado reais.

## Próximos passos sugeridos

1. **Painel admin** com autenticação — hoje os endpoints de escrita (`POST`/`PUT`/`DELETE`) estão
   abertos, o que é aceitável rodando localmente, mas precisa de proteção antes de publicar.
2. **Coleta automatizada** das promoções (APIs de afiliados / rotinas agendadas).
3. **Relatórios de cliques** por período, loja e oferta, aproveitando a tabela `Clicks`.
4. **Deploy**: API em qualquer host .NET e front-end estático (`npm run build`), migrando o SQLite
   para PostgreSQL ou SQL Server quando o volume justificar.
