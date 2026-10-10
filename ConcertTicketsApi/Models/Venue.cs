namespace ConcertTicketsApi.Models;

public sealed class Venue
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public ICollection<ConcertEvent> Events { get; set; } = new List<ConcertEvent>();
}
