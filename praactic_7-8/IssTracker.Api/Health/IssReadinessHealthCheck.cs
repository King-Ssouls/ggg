using IssTracker.Api.Data;
using IssTracker.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace IssTracker.Api.Health;

// Готовность зависит от возраста последнего сохранённого измерения.
public class IssReadinessHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly int _maxAgeMinutes;

    // Получает фабрику областей и предел возраста данных.
    public IssReadinessHealthCheck(IServiceScopeFactory scopeFactory, IOptions<IssApiOptions> options)
    {
        _scopeFactory = scopeFactory;
        _maxAgeMinutes = options.Value.MaxDataAgeMinutes;
    }

    // Проверяет только SQLite и последнюю запись; внешний API не вызывается.
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<IssDbContext>();
            var capturedAt = await database.Positions
                .OrderByDescending(position => position.CapturedAtUtc)
                .Select(position => (DateTime?)position.CapturedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (!capturedAt.HasValue)
            {
                return HealthCheckResult.Unhealthy("Пока нет сохранённых измерений МКС.");
            }

            var age = DateTime.UtcNow - DateTime.SpecifyKind(capturedAt.Value, DateTimeKind.Utc);
            if (age > TimeSpan.FromMinutes(_maxAgeMinutes))
            {
                return HealthCheckResult.Unhealthy("Последнее измерение МКС устарело.");
            }

            return HealthCheckResult.Healthy("Есть свежее измерение МКС.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("Не удалось проверить последнее измерение.");
        }
    }
}
