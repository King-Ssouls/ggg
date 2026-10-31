using IssTracker.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace IssTracker.Api.Data.Migrations;

[DbContext(typeof(IssDbContext))]
[Migration("20261031000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    // Создаёт таблицу измерений и уникальный индекс времени.
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Positions",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SatelliteId = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Latitude = table.Column<double>(type: "REAL", nullable: false),
                Longitude = table.Column<double>(type: "REAL", nullable: false),
                AltitudeKm = table.Column<double>(type: "REAL", nullable: false),
                VelocityKph = table.Column<double>(type: "REAL", nullable: false),
                Visibility = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                CapturedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                SavedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Positions", position => position.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Positions_CapturedAtUtc",
            table: "Positions",
            column: "CapturedAtUtc",
            unique: true);
    }

    // Удаляет таблицу при откате миграции.
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Positions");
    }

    // Описывает модель базы после применения миграции.
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "8.0.20");
        modelBuilder.HasAnnotation("Relational:MaxIdentifierLength", 64);

        modelBuilder.Entity("IssTracker.Api.Models.IssPosition", entity =>
        {
            entity.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            entity.Property<double>("AltitudeKm").HasColumnType("REAL");
            entity.Property<DateTime>("CapturedAtUtc").HasColumnType("TEXT");
            entity.Property<double>("Latitude").HasColumnType("REAL");
            entity.Property<double>("Longitude").HasColumnType("REAL");
            entity.Property<string>("Name").IsRequired().HasMaxLength(100).HasColumnType("TEXT");
            entity.Property<int>("SatelliteId").HasColumnType("INTEGER");
            entity.Property<DateTime>("SavedAtUtc").HasColumnType("TEXT");
            entity.Property<double>("VelocityKph").HasColumnType("REAL");
            entity.Property<string>("Visibility").IsRequired().HasMaxLength(30).HasColumnType("TEXT");

            entity.HasKey("Id");
            entity.HasIndex("CapturedAtUtc").IsUnique();
            entity.ToTable("Positions");
        });
#pragma warning restore 612, 618
    }
}
