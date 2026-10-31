namespace ParkingApi.Models;

// Сущность парковочного места хранится в таблице ParkingSpots и связана с его бронями
public sealed class ParkingSpot
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public bool IsResidentsOnly { get; set; }

    public ICollection<ParkingBooking> Bookings { get; set; } = new List<ParkingBooking>();
}
