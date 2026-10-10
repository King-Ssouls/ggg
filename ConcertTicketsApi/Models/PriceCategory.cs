namespace ConcertTicketsApi.Models;

public sealed class PriceCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int SectorId { get; set; }

    public Sector Sector { get; set; } = null!;
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
