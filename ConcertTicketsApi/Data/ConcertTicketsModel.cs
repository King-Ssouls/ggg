using Microsoft.EntityFrameworkCore;
using ConcertTicketsApi.Models;

namespace ConcertTicketsApi.Data;

/// <summary>Shared EF mapping. The boolean switches describe the schema before and after migration two.</summary>
public static class ConcertTicketsModel
{
    public static void Configure(ModelBuilder modelBuilder, bool hasSectorVersion, bool hasFinalIndexes)
    {
        modelBuilder.Entity<Venue>(entity =>
        {
            entity.ToTable("Venues");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(120);
            entity.Property(x => x.Address).IsRequired().HasMaxLength(240);
        });

        modelBuilder.Entity<Sponsor>(entity =>
        {
            entity.ToTable("Sponsors");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(120);
        });

        modelBuilder.Entity<ConcertEvent>(entity =>
        {
            entity.ToTable("ConcertEvents", table => table.HasCheckConstraint(
                "CK_ConcertEvents_Status", "\"Status\" IN ('Scheduled', 'Cancelled')"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(160);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(x => x.StartsAt).IsRequired();
            entity.HasOne(x => x.Venue).WithMany(x => x.Events).HasForeignKey(x => x.VenueId)
                .OnDelete(DeleteBehavior.Restrict).IsRequired();
            entity.HasOne(x => x.Sponsor).WithMany(x => x.Events).HasForeignKey(x => x.SponsorId)
                .OnDelete(DeleteBehavior.SetNull).IsRequired(false);
            entity.HasIndex(x => x.VenueId);
            entity.HasIndex(x => x.SponsorId);
            if (hasFinalIndexes) entity.HasIndex(x => new { x.Status, x.StartsAt, x.Id });
        });

        modelBuilder.Entity<Sector>(entity =>
        {
            entity.ToTable("Sectors", table =>
            {
                table.HasCheckConstraint("CK_Sectors_Capacity", "\"Capacity\" > 0");
                table.HasCheckConstraint("CK_Sectors_SoldCount", "\"SoldCount\" >= 0 AND \"SoldCount\" <= \"Capacity\"");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(80);
            if (hasSectorVersion)
                entity.Property(x => x.Version).IsConcurrencyToken().ValueGeneratedNever();
            else
                entity.Ignore(x => x.Version);
            entity.HasOne(x => x.ConcertEvent).WithMany(x => x.Sectors).HasForeignKey(x => x.ConcertEventId)
                .OnDelete(DeleteBehavior.Restrict).IsRequired();
            entity.HasIndex(x => new { x.ConcertEventId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<PriceCategory>(entity =>
        {
            entity.ToTable("PriceCategories", table => table.HasCheckConstraint("CK_PriceCategories_Price", "\"Price\" >= 0"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(80);
            entity.Property(x => x.Price).HasPrecision(10, 2);
            entity.HasOne(x => x.Sector).WithMany(x => x.PriceCategories).HasForeignKey(x => x.SectorId)
                .OnDelete(DeleteBehavior.Restrict).IsRequired();
            entity.HasIndex(x => x.SectorId);
        });

        modelBuilder.Entity<Buyer>(entity =>
        {
            entity.ToTable("Buyers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.LastName).IsRequired().HasMaxLength(80);
            entity.Property(x => x.FirstName).IsRequired().HasMaxLength(80);
            entity.Property(x => x.Email).IsRequired().HasMaxLength(254);
            if (hasFinalIndexes) entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Tickets", table =>
            {
                table.HasCheckConstraint("CK_Tickets_Status", "\"Status\" IN ('Sold', 'Refunded')");
                table.HasCheckConstraint("CK_Tickets_SoldPrice", "\"SoldPrice\" >= 0");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Number).IsRequired().HasMaxLength(40);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(x => x.SoldAt).IsRequired();
            entity.Property(x => x.SoldPrice).HasPrecision(10, 2);
            entity.Property(x => x.InternalBarcode).IsRequired().HasMaxLength(64);
            entity.HasOne(x => x.Sector).WithMany(x => x.Tickets).HasForeignKey(x => x.SectorId)
                .OnDelete(DeleteBehavior.Restrict).IsRequired();
            entity.HasOne(x => x.PriceCategory).WithMany(x => x.Tickets).HasForeignKey(x => x.PriceCategoryId)
                .OnDelete(DeleteBehavior.Restrict).IsRequired();
            entity.HasOne(x => x.Buyer).WithMany(x => x.Tickets).HasForeignKey(x => x.BuyerId)
                .OnDelete(DeleteBehavior.Restrict).IsRequired();
            if (hasFinalIndexes) entity.HasIndex(x => x.Number).IsUnique();
            entity.HasIndex(x => x.SectorId);
            entity.HasIndex(x => x.PriceCategoryId);
            entity.HasIndex(x => x.BuyerId);
        });
    }
}
