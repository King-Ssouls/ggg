namespace ConcertTicketsApi.Models;

public sealed class Buyer
{
    public int Id { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
