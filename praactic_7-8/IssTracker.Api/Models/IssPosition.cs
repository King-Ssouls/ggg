namespace IssTracker.Api.Models;

// Измерение положения МКС, сохранённое в нашей базе.
public class IssPosition
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
