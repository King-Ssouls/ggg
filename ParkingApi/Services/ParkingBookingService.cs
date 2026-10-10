using Microsoft.EntityFrameworkCore;
using ParkingApi.Data;
using ParkingApi.DTOs;
using ParkingApi.Models;

namespace ParkingApi.Services;

public enum BookingFailureKind
{
    None,
    BusinessRule,
    NotFound
}

public sealed record BookingOperationResult(ParkingBooking? Booking, BookingFailureKind Failure, string? Error)
{
    public static BookingOperationResult Success(ParkingBooking booking) => new(booking, BookingFailureKind.None, null);
    public static BookingOperationResult Conflict(string error) => new(null, BookingFailureKind.BusinessRule, error);
    public static BookingOperationResult Missing() => new(null, BookingFailureKind.NotFound, "Бронь не найдена.");
}

public sealed class ParkingBookingService(AppDbContext db)
{
    public async Task<BookingOperationResult> CreateAsync(CreateParkingBookingDto dto, CancellationToken cancellationToken)
    {
        var spot = await db.ParkingSpots.SingleOrDefaultAsync(x => x.Id == dto.ParkingSpotId, cancellationToken);
        if (spot is null)
        {
            return BookingOperationResult.Conflict("Машиноместо с таким идентификатором не найдено.");
        }

        var accessError = CheckResidentAccess(spot, dto.HasResidentSubscription);
        if (accessError is not null)
        {
            return BookingOperationResult.Conflict(accessError);
        }

        if (await IsOccupiedAsync(spot.Id, dto.StartAt, dto.EndAt, null, cancellationToken))
        {
            return BookingOperationResult.Conflict("Машиноместо уже занято в выбранный интервал.");
        }

        var booking = new ParkingBooking
        {
            ParkingSpotId = spot.Id,
            ParkingSpot = spot,
            VehiclePlate = dto.VehiclePlate.Trim().ToUpperInvariant(),
            StartAt = dto.StartAt,
            EndAt = dto.EndAt,
            HasResidentSubscription = dto.HasResidentSubscription
        };

        db.ParkingBookings.Add(booking);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return BookingOperationResult.Success(booking);
        }
        catch (DbUpdateException)
        {
            // Covers a foreign-key or other database constraint race without exposing provider details.
            return BookingOperationResult.Conflict("Бронь конфликтует с текущими данными парковки.");
        }
    }

    public async Task<BookingOperationResult> UpdateAsync(
        int id,
        UpdateParkingBookingDto dto,
        CancellationToken cancellationToken)
    {
        var booking = await db.ParkingBookings
            .Include(x => x.ParkingSpot)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (booking is null)
        {
            return BookingOperationResult.Missing();
        }

        var accessError = CheckResidentAccess(booking.ParkingSpot, dto.HasResidentSubscription);
        if (accessError is not null)
        {
            return BookingOperationResult.Conflict(accessError);
        }

        if (await IsOccupiedAsync(booking.ParkingSpotId, dto.StartAt, dto.EndAt, booking.Id, cancellationToken))
        {
            return BookingOperationResult.Conflict("Машиноместо уже занято в выбранный интервал.");
        }

        booking.VehiclePlate = dto.VehiclePlate.Trim().ToUpperInvariant();
        booking.StartAt = dto.StartAt;
        booking.EndAt = dto.EndAt;
        booking.HasResidentSubscription = dto.HasResidentSubscription;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return BookingOperationResult.Success(booking);
        }
        catch (DbUpdateException)
        {
            return BookingOperationResult.Conflict("Бронь конфликтует с текущими данными парковки.");
        }
    }

    private Task<bool> IsOccupiedAsync(
        int spotId,
        DateTime startAt,
        DateTime endAt,
        int? excludedBookingId,
        CancellationToken cancellationToken) => db.ParkingBookings.AnyAsync(booking =>
            booking.ParkingSpotId == spotId &&
            (excludedBookingId == null || booking.Id != excludedBookingId) &&
            booking.StartAt < endAt &&
            booking.EndAt > startAt,
            cancellationToken);

    private static string? CheckResidentAccess(ParkingSpot spot, bool hasSubscription) =>
        spot.IsResidentsOnly && !hasSubscription
            ? "Для бронирования этого места нужен абонемент резидента."
            : null;
}
