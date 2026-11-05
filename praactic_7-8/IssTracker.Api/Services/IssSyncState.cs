using IssTracker.Api.Models;

namespace IssTracker.Api.Services;

// Хранит короткое состояние синхронизации для status и readiness.
public class IssSyncState
{
    private readonly object _lock = new();
    private DateTimeOffset? _lastAttemptUtc;
    private DateTimeOffset? _lastSuccessUtc;
    private string? _lastError;
    private int _consecutiveFailures;

    // Запоминает начало попытки.
    public void MarkAttempt(DateTimeOffset time)
    {
        lock (_lock)
        {
            _lastAttemptUtc = time;
        }
    }

    // Запоминает успешную синхронизацию и обнуляет счётчик ошибок.
    public void MarkSuccess(DateTimeOffset time)
    {
        lock (_lock)
        {
            _lastSuccessUtc = time;
            _lastError = null;
            _consecutiveFailures = 0;
        }
    }

    // Сохраняет ошибку, не меняя время последнего успеха.
    public void MarkFailure(string error)
    {
        lock (_lock)
        {
            _lastError = error;
            _consecutiveFailures++;
        }
    }

    // Возвращает копию состояния для API.
    public IssStatusResponse GetSnapshot()
    {
        lock (_lock)
        {
            return new IssStatusResponse
            {
                LastAttemptUtc = _lastAttemptUtc,
                LastSuccessUtc = _lastSuccessUtc,
                LastError = _lastError,
                ConsecutiveFailures = _consecutiveFailures
            };
        }
    }
}
