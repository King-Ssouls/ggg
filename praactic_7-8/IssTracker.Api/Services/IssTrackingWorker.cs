using IssTracker.Api.Options;
using Microsoft.Extensions.Options;

namespace IssTracker.Api.Services;

// Периодически запускает обновление положения МКС в фоне.
public class IssTrackingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly int _intervalSeconds;
    private readonly ILogger<IssTrackingWorker> _logger;

    // Сохраняет только безопасные для Singleton зависимости.
    public IssTrackingWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<IssApiOptions> options,
        ILogger<IssTrackingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _intervalSeconds = options.Value.UpdateIntervalSeconds;
        _logger = logger;
    }

    // Выполняет первую синхронизацию сразу, затем ждёт PeriodicTimer.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ISS tracking worker started");

        try
        {
            await RunOneIterationAsync(stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_intervalSeconds));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunOneIterationAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Хост остановил приложение — завершаемся спокойно.
        }
        finally
        {
            _logger.LogInformation("ISS tracking worker stopped");
        }
    }

    // Создаёт отдельную область зависимостей для каждой фоновой итерации.
    private async Task RunOneIterationAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var syncService = scope.ServiceProvider.GetRequiredService<IssSyncService>();
        await syncService.RunOnceAsync(cancellationToken);
    }
}
