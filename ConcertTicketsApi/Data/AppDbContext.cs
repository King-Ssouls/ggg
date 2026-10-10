using Microsoft.EntityFrameworkCore;
using ConcertTicketsApi.Models;

namespace ConcertTicketsApi.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Sponsor> Sponsors => Set<Sponsor>();
    public DbSet<ConcertEvent> ConcertEvents => Set<ConcertEvent>();
    public DbSet<Sector> Sectors => Set<Sector>();
    public DbSet<PriceCategory> PriceCategories => Set<PriceCategory>();
    public DbSet<Buyer> Buyers => Set<Buyer>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        ConcertTicketsModel.Configure(modelBuilder, hasSectorVersion: true, hasFinalIndexes: true);
}
