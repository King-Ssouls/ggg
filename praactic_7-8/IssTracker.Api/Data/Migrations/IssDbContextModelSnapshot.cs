using IssTracker.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace IssTracker.Api.Data.Migrations;

[DbContext(typeof(IssDbContext))]
public partial class IssDbContextModelSnapshot : ModelSnapshot
{
    // Сохраняет описание модели для следующих миграций EF Core.
    protected override void BuildModel(ModelBuilder modelBuilder)
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
