using System.ComponentModel.DataAnnotations;

namespace IssTracker.Api.Options;

// Настройки внешнего API и частоты обновления положения МКС.
public class IssApiOptions
{
    public const string SectionName = "IssApi";

    [Required, Url]
    public string BaseUrl { get; set; } = "";

    [Range(1, int.MaxValue)]
    public int SatelliteId { get; set; }

    [Range(5, 3600)]
    public int UpdateIntervalSeconds { get; set; }

    [Range(1, 30)]
    public int RequestTimeoutSeconds { get; set; }

    [Range(1, 1440)]
    public int MaxDataAgeMinutes { get; set; }
}
