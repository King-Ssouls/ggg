using System.ComponentModel.DataAnnotations;
using ParkingApi.Validation;

namespace ParkingApi.DTOs;

public sealed class CreateParkingSpotDto
{
    [Required(ErrorMessage = "Укажите номер машиноместа.")]
    [ParkingSpotNumber]
    public string Number { get; init; } = string.Empty;

    [Required(ErrorMessage = "Укажите зону парковки.")]
    [RegularExpression("^[ABC]$", ErrorMessage = "Зона должна быть A, B или C.")]
    public string Zone { get; init; } = string.Empty;

    public bool IsResidentsOnly { get; init; }
}

// Номер машиноместа после создания неизменяемый: DTO обновления меняет только зону и режим доступа.
public sealed class UpdateParkingSpotDto
{
    [Required(ErrorMessage = "Укажите зону парковки.")]
    [RegularExpression("^[ABC]$", ErrorMessage = "Зона должна быть A, B или C.")]
    public string Zone { get; init; } = string.Empty;

    public bool IsResidentsOnly { get; init; }
}

public sealed record ParkingSpotResponse(int Id, string Number, string Zone, bool IsResidentsOnly);
