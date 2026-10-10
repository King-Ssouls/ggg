namespace ConcertTicketsApi.Models;

public sealed class Sector
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int SoldCount { get; set; }
    public Guid Version { get; set; }
    public int ConcertEventId { get; set; }

    public ConcertEvent ConcertEvent { get; set; } = null!;
    public ICollection<PriceCategory> PriceCategories { get; set; } = new List<PriceCategory>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
