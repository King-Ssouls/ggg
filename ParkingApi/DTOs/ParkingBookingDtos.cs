namespace ParkingApi.DTOs;

// Поля запросов проверяются валидаторами FluentValidation перед вызовом контроллера
public sealed class CreateParkingBookingDto
{
    public int ParkingSpotId { get; init; }
    public string VehiclePlate { get; init; } = string.Empty;
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public bool HasResidentSubscription { get; init; }
}

// При изменении брони машиноместо остаётся прежним
public sealed class UpdateParkingBookingDto
{
    public string VehiclePlate { get; init; } = string.Empty;
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public bool HasResidentSubscription { get; init; }
}

// Ответ объединяет сведения о брони с номером и зоной связанного машиноместа
public sealed record ParkingBookingResponse(
    int Id,
    int ParkingSpotId,
    string ParkingSpotNumber,
    string Zone,
    string VehiclePlate,
    DateTime StartAt,
    DateTime EndAt,
    bool HasResidentSubscription);
