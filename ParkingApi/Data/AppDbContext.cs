using Microsoft.EntityFrameworkCore;
using ParkingApi.Models;

namespace ParkingApi.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Эти наборы EF Core связывает с таблицами мест и броней в SQLite
    public DbSet<ParkingSpot> ParkingSpots => Set<ParkingSpot>();
    public DbSet<ParkingBooking> ParkingBookings => Set<ParkingBooking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Здесь заданы ключи, ограничения полей, связь брони с местом и индексы базы
        modelBuilder.Entity<ParkingSpot>(entity =>
        {
            entity.ToTable("ParkingSpots");
            entity.HasKey(spot => spot.Id);
            entity.Property(spot => spot.Number).IsRequired().HasMaxLength(4);
            entity.Property(spot => spot.Zone).IsRequired().HasMaxLength(1);
            entity.HasIndex(spot => spot.Number).IsUnique();
        });

        modelBuilder.Entity<ParkingBooking>(entity =>
        {
            entity.ToTable("ParkingBookings");
            entity.HasKey(booking => booking.Id);
            entity.Property(booking => booking.VehiclePlate).IsRequired().HasMaxLength(15);
            entity.HasOne(booking => booking.ParkingSpot)
                .WithMany(spot => spot.Bookings)
                .HasForeignKey(booking => booking.ParkingSpotId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
            entity.HasIndex(booking => new { booking.ParkingSpotId, booking.StartAt, booking.EndAt });
        });
    }
}
