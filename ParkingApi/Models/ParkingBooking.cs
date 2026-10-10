namespace ParkingApi.Models;

public sealed class ParkingBooking
{
    public int Id { get; set; }
    public int ParkingSpotId { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool HasResidentSubscription { get; set; }

    public ParkingSpot ParkingSpot { get; set; } = null!;
}
