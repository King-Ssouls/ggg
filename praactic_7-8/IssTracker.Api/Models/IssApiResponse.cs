using System.Text.Json.Serialization;

namespace IssTracker.Api.Models;

// DTO для JSON внешнего сервиса. Это не таблица базы данных.
public class IssApiResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("altitude")]
    public double AltitudeKm { get; set; }

    [JsonPropertyName("velocity")]
    public double VelocityKph { get; set; }

    [JsonPropertyName("visibility")]
    public string? Visibility { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}
