using System.Threading.RateLimiting;
using MegaDescontao.Api.Data;
using MegaDescontao.Api.Endpoints;
using MegaDescontao.Api.Marketplaces;
using MegaDescontao.Api.Marketplaces.Providers;
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

// Providers de marketplace. Os oficiais ficam registrados mesmo sem credencial: eles se
// declaram "não configurados" e o importador os ignora, então habilitar depois é só
// preencher a configuração — nenhuma mudança de código.
var marketplaces = builder.Configuration.GetSection("Marketplaces");
builder.Services.AddSingleton<IMarketplaceProvider>(_ =>
    new MercadoLivreProvider(marketplaces.GetSection("MercadoLivre").Get<MarketplaceCredentials>() ?? new()));
builder.Services.AddSingleton<IMarketplaceProvider>(_ =>
    new ShopeeProvider(marketplaces.GetSection("Shopee").Get<MarketplaceCredentials>() ?? new()));
builder.Services.AddSingleton<IMarketplaceProvider>(_ =>
    new TemuProvider(marketplaces.GetSection("Temu").Get<MarketplaceCredentials>() ?? new()));

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
    db.Database.Migrate();

    // WAL deixa a leitura acontecer durante uma escrita. Sem isso, quando o coletor
    // estiver gravando ofertas a vitrine começa a receber "database is locked".
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
    db.Database.ExecuteSqlRaw("PRAGMA synchronous=NORMAL;");

    SeedData.EnsureSeeded(db);
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

app.Run();
