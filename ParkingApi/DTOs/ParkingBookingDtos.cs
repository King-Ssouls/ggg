namespace ParkingApi.DTOs;

// Эти DTO намеренно не используют Data Annotations: их правила заданы FluentValidation.
public sealed class CreateParkingBookingDto
{
    public int ParkingSpotId { get; init; }
    public string VehiclePlate { get; init; } = string.Empty;
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public bool HasResidentSubscription { get; init; }
}

// При изменении брони машиноместо остаётся прежним.
public sealed class UpdateParkingBookingDto
{
    public string VehiclePlate { get; init; } = string.Empty;
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public bool HasResidentSubscription { get; init; }
}

public sealed record ParkingBookingResponse(
    int Id,
    int ParkingSpotId,
    string ParkingSpotNumber,
    string Zone,
    string VehiclePlate,
    DateTime StartAt,
    DateTime EndAt,
    bool HasResidentSubscription);
