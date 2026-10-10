using System.ComponentModel.DataAnnotations;
using ConcertTicketsApi.Models;

namespace ConcertTicketsApi.Contracts;

public sealed class SellTicketDto
{
    [Range(1, int.MaxValue)]
    public int SectorId { get; init; }

    [Range(1, int.MaxValue)]
    public int PriceCategoryId { get; init; }

    [Range(1, int.MaxValue)]
    public int BuyerId { get; init; }
}

public sealed record TicketDto(
    int Id,
    string Number,
    TicketStatus Status,
    DateTime SoldAt,
    decimal SoldPrice,
    int SectorId,
    string SectorName,
    int ConcertEventId,
    string EventTitle,
    int BuyerId,
    string BuyerName,
    int PriceCategoryId,
    string PriceCategoryName);

public sealed record TicketWithSectorDto(
    int Id,
    string Number,
    TicketStatus Status,
    int SectorId,
    string SectorName);

public sealed record TicketDetailsDto(
    int Id,
    string Number,
    TicketStatus Status,
    DateTime SoldAt,
    decimal SoldPrice,
    int SectorId,
    string SectorName,
    int ConcertEventId,
    string EventTitle,
    DateTime EventStartsAt,
    ConcertEventStatus EventStatus,
    int VenueId,
    string VenueName,
    string VenueAddress,
    int? SponsorId,
    string? SponsorName,
    int BuyerId,
    string BuyerName,
    string BuyerEmail,
    int PriceCategoryId,
    string PriceCategoryName);

public sealed record TicketPageDto(int Page, int PageSize, int TotalCount, IReadOnlyList<TicketDto> Items);
public sealed record SaleResultDto(TicketDto Ticket, int RemainingSeats);

public sealed class SectorRevenueDto
{
    public int SectorId { get; set; }
    public string SectorName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int SoldCount { get; set; }
    public int RemainingSeats { get; set; }
    public decimal Revenue { get; set; }
}
