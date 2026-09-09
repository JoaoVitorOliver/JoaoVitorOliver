namespace MegaDescontao.Api.Marketplaces;

/// Roda junto com a API e tira do ar as promoções cujo prazo venceu: toda oferta ativa
/// com ExpiresAt no passado vira Expired.
///
/// A vitrine já ignora oferta vencida na hora da consulta, então o site nunca mostra
/// promoção morta. O que este serviço faz é deixar o BANCO honesto — sem isso, o status
/// gravado mentiria, e o relatório de cliques e a tela do admin mostrariam como ativa
/// uma promoção que já acabou.
public class OfferExpirationService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<OfferExpirationService> logger) : BackgroundService
{
    private const int DefaultIntervalMinutes = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = Math.Max(configuration.GetValue<int?>("Offers:ExpirationCheckMinutes") ?? DefaultIntervalMinutes, 1);

        logger.LogInformation("Verificação de ofertas vencidas ativa, a cada {Minutes} minuto(s)", minutes);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutes));

        try
        {
            // Uma passada logo no start: o site pode ter ficado parado enquanto promoções venciam.
            await ExpireAsync(stoppingToken);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ExpireAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Verificação de ofertas vencidas encerrada.");
        }
    }

    private async Task ExpireAsync(CancellationToken cancellationToken)
    {
        try
        {
            // O DbContext é scoped e este serviço é singleton: um escopo novo a cada passada.
            await using var scope = scopeFactory.CreateAsyncScope();
            var importService = scope.ServiceProvider.GetRequiredService<ProductImportService>();

            var expired = await importService.ExpireOutdatedOffersAsync(cancellationToken);

            if (expired > 0)
            {
                logger.LogInformation("{Count} oferta(s) venceram e saíram da vitrine.", expired);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Desde o .NET 6, exceção não tratada num BackgroundService derruba a aplicação
            // inteira. Uma falha ao expirar não pode levar a vitrine junto.
            logger.LogError(ex, "Falha ao expirar ofertas vencidas. Tentando de novo no próximo ciclo.");
        }
    }
}
