namespace ConcertTicketsApi.Models;

public sealed class ConcertEvent
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public ConcertEventStatus Status { get; set; }
    public int VenueId { get; set; }
    public int? SponsorId { get; set; }

    public Venue Venue { get; set; } = null!;
    public Sponsor? Sponsor { get; set; }
    public ICollection<Sector> Sectors { get; set; } = new List<Sector>();
}
