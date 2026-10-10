using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingApi.Controllers;
using ParkingApi.Data;
using ParkingApi.DTOs;
using ParkingApi.Middleware;
using ParkingApi.Models;
using ParkingApi.Services;

namespace ParkingApi.Controllers;

[ApiController]
[Route("api/parking-bookings")]
public sealed class ParkingBookingsController(AppDbContext db, ParkingBookingService bookingService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ParkingBookingResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ParkingBookingResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var bookings = await db.ParkingBookings.AsNoTracking()
            .Include(x => x.ParkingSpot)
            .OrderBy(x => x.StartAt)
            .ToListAsync(cancellationToken);
        return Ok(bookings.Select(ToResponse).ToList());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ParkingBookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParkingBookingResponse>> GetBookingById(int id, CancellationToken cancellationToken)
    {
        var booking = await db.ParkingBookings.AsNoTracking().Include(x => x.ParkingSpot)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return booking is null
            ? NotFound(ApiProblemDetails.Create(HttpContext, 404, "Бронь не найдена", "Проверьте идентификатор брони."))
            : Ok(ToResponse(booking));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ParkingBookingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ParkingBookingResponse>> Create(
        [FromBody] CreateParkingBookingDto dto,
        [FromHeader(Name = "X-Correlation-ID")] string? correlationId,
        CancellationToken cancellationToken)
    {
        _ = correlationId;
        var result = await bookingService.CreateAsync(dto, cancellationToken);
        if (result.Failure == BookingFailureKind.BusinessRule)
        {
            return Conflict(ApiProblemDetails.Create(HttpContext, 409, "Невозможно создать бронь", result.Error!));
        }

        var booking = result.Booking!;
        return CreatedAtAction(nameof(GetBookingById), new { id = booking.Id }, ToResponse(booking));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ParkingBookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ParkingBookingResponse>> Update(
        int id,
        [FromBody] UpdateParkingBookingDto dto,
        [FromHeader(Name = "X-Correlation-ID")] string? correlationId,
        CancellationToken cancellationToken)
    {
        _ = correlationId;
        var result = await bookingService.UpdateAsync(id, dto, cancellationToken);
        if (result.Failure == BookingFailureKind.NotFound)
        {
            return NotFound(ApiProblemDetails.Create(HttpContext, 404, "Бронь не найдена", result.Error!));
        }

        if (result.Failure == BookingFailureKind.BusinessRule)
        {
            return Conflict(ApiProblemDetails.Create(HttpContext, 409, "Невозможно изменить бронь", result.Error!));
        }

        return Ok(ToResponse(result.Booking!));
    }

    private static ParkingBookingResponse ToResponse(ParkingBooking booking) => new(
        booking.Id,
        booking.ParkingSpotId,
        booking.ParkingSpot.Number,
        booking.ParkingSpot.Zone,
        booking.VehiclePlate,
        booking.StartAt,
        booking.EndAt,
        booking.HasResidentSubscription);
}
