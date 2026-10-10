namespace ConcertTicketsApi.Models;

public sealed class Ticket
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public DateTime SoldAt { get; set; }
    public decimal SoldPrice { get; set; }
    public string InternalBarcode { get; set; } = string.Empty;
    public int SectorId { get; set; }
    public int PriceCategoryId { get; set; }
    public int BuyerId { get; set; }

    public Sector Sector { get; set; } = null!;
    public PriceCategory PriceCategory { get; set; } = null!;
    public Buyer Buyer { get; set; } = null!;
}
