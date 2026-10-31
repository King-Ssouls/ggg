namespace IssTracker.Api.Services;

// Итог одной попытки: успех, ошибка или занятая синхронизация.
public enum SyncOutcome
{
    Succeeded,
    Failed,
    Busy
}

// Данные, которые сервис возвращает контроллеру или worker.
public class SyncRunResult
{
    public SyncOutcome Outcome { get; set; }
    public DateTimeOffset? CapturedAtUtc { get; set; }
    public bool SavedNewPosition { get; set; }
    public string? ErrorMessage { get; set; }
}
