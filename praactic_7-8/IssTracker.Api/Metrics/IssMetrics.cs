using Prometheus;

namespace IssTracker.Api.Metrics;

// Пользовательские метрики синхронизации положения МКС.
public static class IssMetrics
{
    public static readonly Counter SyncAttempts = Prometheus.Metrics.CreateCounter(
        "iss_sync_attempts_total",
        "Сколько раз приложение запускало синхронизацию.");

    public static readonly Counter SyncFailures = Prometheus.Metrics.CreateCounter(
        "iss_sync_failures_total",
        "Сколько синхронизаций завершилось ошибкой.");

    public static readonly Histogram SyncDuration = Prometheus.Metrics.CreateHistogram(
        "iss_sync_duration_seconds",
        "Длительность синхронизации в секундах.");
}
