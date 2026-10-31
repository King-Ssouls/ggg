namespace IssTracker.Api.Models;

// Одна запись положения для ответа клиенту.
public class IssPositionView
{
    public int Id { get; set; }
    public int SatelliteId { get; set; }
    public string Name { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double AltitudeKm { get; set; }
    public double VelocityKph { get; set; }
    public string Visibility { get; set; } = "";
    public DateTime CapturedAtUtc { get; set; }
    public DateTime SavedAtUtc { get; set; }
}

// Последняя запись и признак, не устарели ли данные.
public class IssLatestResponse
{
    public IssPositionView Position { get; set; } = new();
    public double AgeSeconds { get; set; }
    public bool IsStale { get; set; }
}

// Состояние последней попытки синхронизации.
public class IssStatusResponse
{
    public DateTimeOffset? LastAttemptUtc { get; set; }
    public DateTimeOffset? LastSuccessUtc { get; set; }
    public string? LastError { get; set; }
    public int ConsecutiveFailures { get; set; }
}
