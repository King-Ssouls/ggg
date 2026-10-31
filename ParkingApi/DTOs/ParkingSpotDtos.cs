using System.ComponentModel.DataAnnotations;
using ParkingApi.Validation;

namespace ParkingApi.DTOs;

// DTO создания отделяет входные данные API от модели, которая хранится в базе
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

// Номер машиноместа после создания неизменяемый, поэтому обновление меняет только зону и режим доступа
public sealed class UpdateParkingSpotDto
{
    [Required(ErrorMessage = "Укажите зону парковки.")]
    [RegularExpression("^[ABC]$", ErrorMessage = "Зона должна быть A, B или C.")]
    public string Zone { get; init; } = string.Empty;

    public bool IsResidentsOnly { get; init; }
}

// Ответ API содержит публичные данные парковочного места
public sealed record ParkingSpotResponse(int Id, string Number, string Zone, bool IsResidentsOnly);
