namespace ConcertTicketsApi.Models;

public sealed class Sponsor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<ConcertEvent> Events { get; set; } = new List<ConcertEvent>();
}
