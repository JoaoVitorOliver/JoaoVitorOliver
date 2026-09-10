using System.Threading.RateLimiting;
using MegaDescontao.Api.Data;
using MegaDescontao.Api.Endpoints;
using MegaDescontao.Api.Marketplaces;
using MegaDescontao.Api.Marketplaces.Providers;
using MegaDescontao.Api.Marketplaces.Providers.Shopee;
using MegaDescontao.Api.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new()
{
    Title = "Mega Descontão API",
    Version = "v1",
    Description = "Vitrine de promoções dos marketplaces com redirecionamento rastreado para links de afiliado.",
}));

const string WebCorsPolicy = "web";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options => options.AddPolicy(WebCorsPolicy, policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(RateLimitPolicies.Redirect, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));

    options.AddPolicy(RateLimitPolicies.Admin, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddScoped<AdminApiKeyFilter>();
builder.Services.AddScoped<ProductImportService>();

// Tira do ar sozinho as promoções cujo prazo venceu.
builder.Services.AddHostedService<OfferExpirationService>();

// Providers de marketplace. Os oficiais ficam registrados mesmo sem credencial: eles se
// declaram "não configurados" e o importador os ignora, então habilitar depois é só
// preencher a configuração — nenhuma mudança de código.
var marketplaces = builder.Configuration.GetSection("Marketplaces");
builder.Services.AddSingleton<IMarketplaceProvider>(_ =>
    new MercadoLivreProvider(marketplaces.GetSection("MercadoLivre").Get<MarketplaceCredentials>() ?? new()));
builder.Services.AddHttpClient(ShopeeProvider.HttpClientName, client =>
    client.Timeout = TimeSpan.FromSeconds(30));

builder.Services.AddSingleton<IMarketplaceProvider>(sp => new ShopeeProvider(
    marketplaces.GetSection("Shopee").Get<ShopeeOptions>() ?? new(),
    sp.GetRequiredService<IHttpClientFactory>(),
    sp.GetRequiredService<ILogger<ShopeeProvider>>()));

foreach (var feed in builder.Configuration.GetSection("Import:JsonFeeds").Get<JsonFeedOptions[]>() ?? [])
{
    if (!Path.IsPathRooted(feed.FilePath))
    {
        feed.FilePath = Path.Combine(builder.Environment.ContentRootPath, feed.FilePath);
    }

    builder.Services.AddSingleton<IMarketplaceProvider>(sp =>
        new JsonFeedProvider(feed, sp.GetRequiredService<ILogger<JsonFeedProvider>>()));
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Só chama Migrate() quando há migration pendente. A partir do EF 9 o Migrate()
    // pega uma trava exclusiva (__EFMigrationsLock) mesmo quando não há nada a aplicar;
    // se o processo morrer nessa janela, a linha de trava fica no arquivo .db e todo
    // startup seguinte fica esperando por ela para sempre, sem erro e sem timeout.
    if (db.Database.GetPendingMigrations().Any())
    {
        db.Database.Migrate();
    }

    // WAL deixa a leitura acontecer durante uma escrita. Sem isso, quando o coletor
    // estiver gravando ofertas a vitrine começa a receber "database is locked".
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
    db.Database.ExecuteSqlRaw("PRAGMA synchronous=NORMAL;");

    // Catálogo de demonstração só em desenvolvimento. Em produção o site começa vazio e
    // é alimentado pelas ofertas reais — publicar 16 produtos fictícios com link de busca
    // seria pior que não ter produto nenhum.
    if (app.Environment.IsDevelopment())
    {
        SeedData.EnsureSeeded(db);
    }
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Mega Descontão API v1"));
}

app.UseCors(WebCorsPolicy);
app.UseRateLimiter();

app.MapProductEndpoints();
app.MapCatalogEndpoints();
app.MapGoEndpoints();
app.MapAdminEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).WithTags("Infra");

// Em produção a vitrine é servida pela própria API (a imagem Docker copia o build do React
// para wwwroot). Um processo só, mesma origem — o que também dispensa CORS. Em
// desenvolvimento a pasta não existe e o Vite continua servindo o front à parte.
if (Directory.Exists(Path.Combine(app.Environment.ContentRootPath, "wwwroot")))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();

    app.MapFallback(context =>
    {
        // Rota de API inexistente tem que responder 404, e não a página do site.
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        }

        context.Response.ContentType = "text/html";
        return context.Response.SendFileAsync(
            Path.Combine(app.Environment.ContentRootPath, "wwwroot", "index.html"));
    });
}

app.Run();
