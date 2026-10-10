using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingApi.Data;
using ParkingApi.DTOs;
using ParkingApi.Models;
using ParkingApi.Middleware;

namespace ParkingApi.Controllers;

[ApiController]
[Route("api/parking-spots")]
public sealed class ParkingSpotsController(AppDbContext db, ILogger<ParkingSpotsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ParkingSpotResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ParkingSpotResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var spots = await db.ParkingSpots.AsNoTracking().OrderBy(x => x.Number).ToListAsync(cancellationToken);
        return Ok(spots.Select(ToResponse).ToList());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ParkingSpotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParkingSpotResponse>> GetSpotById(int id, CancellationToken cancellationToken)
    {
        var spot = await db.ParkingSpots.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return spot is null
            ? NotFound(ApiProblemDetails.Create(HttpContext, 404, "Машиноместо не найдено", "Проверьте идентификатор машиноместа."))
            : Ok(ToResponse(spot));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ParkingSpotResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ParkingSpotResponse>> Create(
        [FromBody] CreateParkingSpotDto dto,
        [FromHeader(Name = "X-Correlation-ID")] string? correlationId,
        CancellationToken cancellationToken)
    {
        _ = correlationId; // Display this header in Swagger; the middleware owns its value.
        if (await db.ParkingSpots.AnyAsync(x => x.Number == dto.Number, cancellationToken))
        {
            return Conflict(ApiProblemDetails.Create(HttpContext, 409, "Машиноместо уже существует", "Номер места должен быть уникальным."));
        }

        var spot = new ParkingSpot { Number = dto.Number, Zone = dto.Zone, IsResidentsOnly = dto.IsResidentsOnly };
        db.ParkingSpots.Add(spot);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            logger.LogInformation(exception, "Parking spot insert conflicted with a database constraint");
            return Conflict(ApiProblemDetails.Create(HttpContext, 409, "Машиноместо уже существует", "Номер места должен быть уникальным."));
        }

        return CreatedAtAction(nameof(GetSpotById), new { id = spot.Id }, ToResponse(spot));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ParkingSpotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParkingSpotResponse>> Update(
        int id,
        [FromBody] UpdateParkingSpotDto dto,
        [FromHeader(Name = "X-Correlation-ID")] string? correlationId,
        CancellationToken cancellationToken)
    {
        _ = correlationId;
        var spot = await db.ParkingSpots.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (spot is null)
        {
            return NotFound(ApiProblemDetails.Create(HttpContext, 404, "Машиноместо не найдено", "Проверьте идентификатор машиноместа."));
        }

        spot.Zone = dto.Zone;
        spot.IsResidentsOnly = dto.IsResidentsOnly;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(spot));
    }

    private static ParkingSpotResponse ToResponse(ParkingSpot spot) =>
        new(spot.Id, spot.Number, spot.Zone, spot.IsResidentsOnly);
}
