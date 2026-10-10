using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConcertTicketsApi.Contracts;
using ConcertTicketsApi.Data;
using ConcertTicketsApi.Models;

namespace ConcertTicketsApi.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<EventPageDto>> GetPage(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] DateTime? from = null,
        [FromQuery] ConcertEventStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = FilterEvents(from, status);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.StartsAt).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new EventListItemDto(x.Id, x.Title, x.StartsAt, x.Status, x.Venue.Name, x.Sponsor == null ? null : x.Sponsor.Name))
            .ToListAsync(cancellationToken);
        return Ok(new EventPageDto(page, pageSize, total, items));
    }

    [HttpGet("after")]
    public async Task<ActionResult<EventCursorPageDto>> GetAfter(
        [FromQuery] DateTime? afterStartsAt,
        [FromQuery] int? afterId,
        [FromQuery] int pageSize = 10,
        [FromQuery] DateTime? from = null,
        [FromQuery] ConcertEventStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = FilterEvents(from, status);
        if (afterStartsAt.HasValue && afterId.HasValue)
        {
            var cursorTime = afterStartsAt.Value;
            var cursorId = afterId.Value;
            query = query.Where(x => x.StartsAt > cursorTime || (x.StartsAt == cursorTime && x.Id > cursorId));
        }

        var rows = await query.OrderBy(x => x.StartsAt).ThenBy(x => x.Id).Take(pageSize + 1)
            .Select(x => new EventListItemDto(x.Id, x.Title, x.StartsAt, x.Status, x.Venue.Name, x.Sponsor == null ? null : x.Sponsor.Name))
            .ToListAsync(cancellationToken);
        var hasNext = rows.Count > pageSize;
        var items = rows.Take(pageSize).ToList();
        var last = hasNext && items.Count > 0 ? items[^1] : null;
        return Ok(new EventCursorPageDto(last?.StartsAt, last?.Id, items));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ConcertEventDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var concert = await db.ConcertEvents.AsNoTracking()
            .Include(x => x.Venue)
            .Include(x => x.Sponsor)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return concert is null
            ? NotFound(ApiProblem.Create(HttpContext, 404, "Событие не найдено", "Проверьте идентификатор события."))
            : Ok(ToDto(concert));
    }

    [HttpPost]
    public async Task<ActionResult<ConcertEventDto>> Create(CreateConcertEventDto dto, CancellationToken cancellationToken)
    {
        var venue = await db.Venues.SingleOrDefaultAsync(x => x.Id == dto.VenueId, cancellationToken);
        if (venue is null)
        {
            return BadRequest(ApiProblem.Create(HttpContext, 400, "Неверная площадка", "Сначала создайте площадку или укажите существующий VenueId."));
        }

        Sponsor? sponsor = null;
        if (dto.SponsorId.HasValue)
        {
            sponsor = await db.Sponsors.SingleOrDefaultAsync(x => x.Id == dto.SponsorId.Value, cancellationToken);
            if (sponsor is null)
            {
                return BadRequest(ApiProblem.Create(HttpContext, 400, "Неверный спонсор", "Указанный SponsorId не найден."));
            }
        }

        var concert = new ConcertEvent
        {
            Title = dto.Title.Trim(), StartsAt = dto.StartsAt, Status = dto.Status, Venue = venue, Sponsor = sponsor
        };
        db.ConcertEvents.Add(concert);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = concert.Id }, ToDto(concert));
    }

    [HttpGet("sql-preview")]
    public ActionResult<object> SqlPreview([FromQuery] DateTime? from = null, [FromQuery] ConcertEventStatus? status = null)
    {
        var sql = FilterEvents(from, status)
            .OrderBy(x => x.StartsAt).ThenBy(x => x.Id)
            .Select(x => new EventListItemDto(x.Id, x.Title, x.StartsAt, x.Status, x.Venue.Name, x.Sponsor == null ? null : x.Sponsor.Name))
            .Take(10).ToQueryString();
        return Ok(new { sql, note = "Список строится на сервере БД, затем проецируется только в поля ответа." });
    }

    [HttpGet("with-sectors")]
    public async Task<ActionResult<object>> WithSectors([FromQuery] int take = 5, CancellationToken cancellationToken = default)
    {
        var events = await db.ConcertEvents.AsNoTracking().OrderBy(x => x.StartsAt).Take(Math.Clamp(take, 1, 20))
            .Include(x => x.Sectors)
            .AsSingleQuery()
            .Select(x => new { x.Id, x.Title, Sectors = x.Sectors.Select(s => new { s.Id, s.Name, s.Capacity, s.SoldCount }).ToList() })
            .ToListAsync(cancellationToken);
        return Ok(events);
    }

    [HttpGet("n-plus-one")]
    public async Task<ActionResult<object>> NPlusOne(CancellationToken cancellationToken)
    {
        // Deliberately inefficient teaching example: one list query and one extra query per event.
        var events = await db.ConcertEvents.AsTracking().OrderBy(x => x.StartsAt).Take(5).ToListAsync(cancellationToken);
        var rows = new List<object>();
        foreach (var concert in events)
        {
            await db.Entry(concert).Collection(x => x.Sectors).LoadAsync(cancellationToken);
            rows.Add(new { concert.Id, concert.Title, SectorCount = concert.Sectors.Count });
        }

        return Ok(new { queryCountHint = events.Count + 1, items = rows, note = "Сравните консольный SQL с /api/events/with-sectors." });
    }

    private IQueryable<ConcertEvent> FilterEvents(DateTime? from, ConcertEventStatus? status)
    {
        var query = db.ConcertEvents.AsNoTracking();
        if (from.HasValue) query = query.Where(x => x.StartsAt >= from.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        return query;
    }

    private static ConcertEventDto ToDto(ConcertEvent concert) => new(
        concert.Id, concert.Title, concert.StartsAt, concert.Status, concert.VenueId,
        new VenueDto(concert.Venue.Id, concert.Venue.Name, concert.Venue.Address),
        concert.SponsorId,
        concert.Sponsor is null ? null : new SponsorDto(concert.Sponsor.Id, concert.Sponsor.Name));
}
