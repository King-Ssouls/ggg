using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ParkingApi.Data;

namespace ParkingApi.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202610100001_InitialCreate")]
public sealed class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ParkingSpots",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                Number = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                Zone = table.Column<string>(type: "TEXT", maxLength: 1, nullable: false),
                IsResidentsOnly = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ParkingSpots", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ParkingBookings",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                ParkingSpotId = table.Column<int>(type: "INTEGER", nullable: false),
                VehiclePlate = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                StartAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                EndAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                HasResidentSubscription = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ParkingBookings", x => x.Id);
                table.ForeignKey(
                    name: "FK_ParkingBookings_ParkingSpots_ParkingSpotId",
                    column: x => x.ParkingSpotId,
                    principalTable: "ParkingSpots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ParkingBookings_ParkingSpotId_StartAt_EndAt",
            table: "ParkingBookings",
            columns: new[] { "ParkingSpotId", "StartAt", "EndAt" });
        migrationBuilder.CreateIndex(
            name: "IX_ParkingSpots_Number",
            table: "ParkingSpots",
            column: "Number",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ParkingBookings");
        migrationBuilder.DropTable(name: "ParkingSpots");
    }

    protected override void BuildTargetModel(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
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
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict)
                .IsRequired();
            entity.Navigation("ParkingSpot");
        });

        modelBuilder.Entity("ParkingApi.Models.ParkingSpot", entity => entity.Navigation("Bookings"));
    }
}
