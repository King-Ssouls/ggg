using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConcertTicketsApi.Contracts;
using ConcertTicketsApi.Data;
using ConcertTicketsApi.Models;
using ConcertTicketsApi.Services;

namespace ConcertTicketsApi.Controllers;

[ApiController]
[Route("api/tickets")]
public sealed class TicketsController(AppDbContext db, TicketSalesService sales) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TicketPageDto>> GetPage(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] TicketStatus? status = null,
        [FromQuery] int? eventId = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = FilterTickets(status, eventId);
        var count = await query.CountAsync(cancellationToken);
        var tickets = await query.OrderByDescending(x => x.SoldAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new TicketDto(x.Id, x.Number, x.Status, x.SoldAt, x.SoldPrice,
                x.SectorId, x.Sector.Name, x.Sector.ConcertEventId, x.Sector.ConcertEvent.Title,
                x.BuyerId, x.Buyer.LastName + " " + x.Buyer.FirstName,
                x.PriceCategoryId, x.PriceCategory.Name))
            .ToListAsync(cancellationToken);
        return Ok(new TicketPageDto(page, pageSize, count, tickets));
    }

    [HttpGet("{id:int}/with-sector")]
    public async Task<ActionResult<TicketWithSectorDto>> GetWithSector(int id, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets.AsNoTracking().Include(x => x.Sector)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return ticket is null
            ? NotFound(ApiProblem.Create(HttpContext, 404, "Билет не найден", "Проверьте идентификатор билета."))
            : Ok(new TicketWithSectorDto(ticket.Id, ticket.Number, ticket.Status, ticket.SectorId, ticket.Sector.Name));
    }

    [HttpGet("{id:int}/details")]
    public async Task<ActionResult<TicketDetailsDto>> GetDetails(int id, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets.AsNoTracking()
            .Include(x => x.Sector).ThenInclude(x => x.ConcertEvent).ThenInclude(x => x.Venue)
            .Include(x => x.Sector).ThenInclude(x => x.ConcertEvent).ThenInclude(x => x.Sponsor)
            .Include(x => x.Buyer)
            .Include(x => x.PriceCategory)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (ticket is null)
        {
            return NotFound(ApiProblem.Create(HttpContext, 404, "Билет не найден", "Проверьте идентификатор билета."));
        }

        var concert = ticket.Sector.ConcertEvent;
        return Ok(new TicketDetailsDto(
            ticket.Id, ticket.Number, ticket.Status, ticket.SoldAt, ticket.SoldPrice,
            ticket.SectorId, ticket.Sector.Name, concert.Id, concert.Title, concert.StartsAt, concert.Status,
            concert.VenueId, concert.Venue.Name, concert.Venue.Address,
            concert.SponsorId, concert.Sponsor?.Name,
            ticket.BuyerId, $"{ticket.Buyer.LastName} {ticket.Buyer.FirstName}", ticket.Buyer.Email,
            ticket.PriceCategoryId, ticket.PriceCategory.Name));
    }

    [HttpPost]
    public async Task<ActionResult<SaleResultDto>> Sell(SellTicketDto dto, CancellationToken cancellationToken)
    {
        var result = await sales.SellAsync(dto, cancellationToken);
        if (result.Failure == TicketCommandFailure.NotFound)
        {
            return NotFound(ApiProblem.Create(HttpContext, 404, "Не найден объект продажи", result.Error!));
        }
        if (result.Failure == TicketCommandFailure.Conflict)
        {
            return Conflict(ApiProblem.Create(HttpContext, 409, "Продажа отклонена", result.Error!));
        }

        return CreatedAtAction(nameof(GetDetails), new { id = result.Ticket!.Id },
            new SaleResultDto(ToDto(result.Ticket), result.RemainingSeats));
    }

    [HttpPost("{id:int}/refund")]
    public async Task<ActionResult<SaleResultDto>> Refund(int id, CancellationToken cancellationToken)
    {
        var result = await sales.RefundAsync(id, cancellationToken);
        if (result.Failure == TicketCommandFailure.NotFound)
        {
            return NotFound(ApiProblem.Create(HttpContext, 404, "Билет не найден", result.Error!));
        }
        if (result.Failure == TicketCommandFailure.Conflict)
        {
            return Conflict(ApiProblem.Create(HttpContext, 409, "Возврат отклонен", result.Error!));
        }

        return Ok(new SaleResultDto(ToDto(result.Ticket!), result.RemainingSeats));
    }

    private IQueryable<Ticket> FilterTickets(TicketStatus? status, int? eventId)
    {
        var query = db.Tickets.AsNoTracking();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (eventId.HasValue) query = query.Where(x => x.Sector.ConcertEventId == eventId.Value);
        return query;
    }

    private static TicketDto ToDto(Ticket ticket) => new(
        ticket.Id, ticket.Number, ticket.Status, ticket.SoldAt, ticket.SoldPrice,
        ticket.SectorId, ticket.Sector.Name, ticket.Sector.ConcertEventId, ticket.Sector.ConcertEvent.Title,
        ticket.BuyerId, $"{ticket.Buyer.LastName} {ticket.Buyer.FirstName}",
        ticket.PriceCategoryId, ticket.PriceCategory.Name);
}
