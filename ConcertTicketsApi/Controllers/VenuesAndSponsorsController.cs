using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConcertTicketsApi.Contracts;
using ConcertTicketsApi.Data;
using ConcertTicketsApi.Models;

namespace ConcertTicketsApi.Controllers;

[ApiController]
[Route("api/venues")]
public sealed class VenuesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VenueDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await db.Venues.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new VenueDto(x.Id, x.Name, x.Address)).ToListAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VenueDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var venue = await db.Venues.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new VenueDto(x.Id, x.Name, x.Address)).SingleOrDefaultAsync(cancellationToken);
        return venue is null ? NotFound(ApiProblem.Create(HttpContext, 404, "Площадка не найдена", "Проверьте идентификатор площадки.")) : Ok(venue);
    }

    [HttpPost]
    public async Task<ActionResult<VenueDto>> Create(CreateVenueDto dto, CancellationToken cancellationToken)
    {
        var venue = new Venue { Name = dto.Name.Trim(), Address = dto.Address.Trim() };
        db.Venues.Add(venue);
        await db.SaveChangesAsync(cancellationToken);
        var response = new VenueDto(venue.Id, venue.Name, venue.Address);
        return CreatedAtAction(nameof(GetById), new { id = venue.Id }, response);
    }
}

[ApiController]
[Route("api/sponsors")]
public sealed class SponsorsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SponsorDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await db.Sponsors.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new SponsorDto(x.Id, x.Name)).ToListAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SponsorDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var sponsor = await db.Sponsors.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new SponsorDto(x.Id, x.Name)).SingleOrDefaultAsync(cancellationToken);
        return sponsor is null ? NotFound(ApiProblem.Create(HttpContext, 404, "Спонсор не найден", "Проверьте идентификатор спонсора.")) : Ok(sponsor);
    }

    [HttpPost]
    public async Task<ActionResult<SponsorDto>> Create(CreateSponsorDto dto, CancellationToken cancellationToken)
    {
        var sponsor = new Sponsor { Name = dto.Name.Trim() };
        db.Sponsors.Add(sponsor);
        await db.SaveChangesAsync(cancellationToken);
        var response = new SponsorDto(sponsor.Id, sponsor.Name);
        return CreatedAtAction(nameof(GetById), new { id = sponsor.Id }, response);
    }

    // The optional event-to-sponsor FK is set to NULL, so deleting a sponsor preserves events.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var sponsor = await db.Sponsors.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (sponsor is null)
        {
            return NotFound(ApiProblem.Create(HttpContext, 404, "Спонсор не найден", "Проверьте идентификатор спонсора."));
        }

        db.Sponsors.Remove(sponsor);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
