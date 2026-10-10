using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ParkingApi.Data;

namespace ParkingApi.Migrations;

[DbContext(typeof(AppDbContext))]
public sealed class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.12");
        modelBuilder.HasAnnotation("Relational:MaxIdentifierLength", 64);

        modelBuilder.Entity("ParkingApi.Models.ParkingSpot", entity =>
        {
            entity.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            entity.Property<bool>("IsResidentsOnly").HasColumnType("INTEGER");
            entity.Property<string>("Number").IsRequired().HasMaxLength(4).HasColumnType("TEXT");
            entity.Property<string>("Zone").IsRequired().HasMaxLength(1).HasColumnType("TEXT");
            entity.HasKey("Id");
            entity.HasIndex("Number").IsUnique();
            entity.ToTable("ParkingSpots");
        });

        modelBuilder.Entity("ParkingApi.Models.ParkingBooking", entity =>
        {
            entity.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            entity.Property<DateTime>("EndAt").HasColumnType("TEXT");
            entity.Property<bool>("HasResidentSubscription").HasColumnType("INTEGER");
            entity.Property<int>("ParkingSpotId").HasColumnType("INTEGER");
            entity.Property<DateTime>("StartAt").HasColumnType("TEXT");
            entity.Property<string>("VehiclePlate").IsRequired().HasMaxLength(15).HasColumnType("TEXT");
            entity.HasKey("Id");
            entity.HasIndex("ParkingSpotId", "StartAt", "EndAt");
            entity.ToTable("ParkingBookings");
        });

        modelBuilder.Entity("ParkingApi.Models.ParkingBooking", entity =>
        {
            entity.HasOne("ParkingApi.Models.ParkingSpot", "ParkingSpot")
                .WithMany("Bookings")
                .HasForeignKey("ParkingSpotId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
            entity.Navigation("ParkingSpot");
        });

        modelBuilder.Entity("ParkingApi.Models.ParkingSpot", entity => entity.Navigation("Bookings"));
    }
}
