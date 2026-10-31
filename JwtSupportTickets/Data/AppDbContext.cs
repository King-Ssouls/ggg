using JwtSupportTickets.Models;
using Microsoft.EntityFrameworkCore;

namespace JwtSupportTickets.Data;

// Описывает таблицы и связи базы данных.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Настраивает уникальность email и связи между таблицами.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>()
            .HasIndex(user => user.Email)
            .IsUnique();

        modelBuilder.Entity<AppUser>()
            .Property(user => user.Email)
            .HasMaxLength(254);

        modelBuilder.Entity<AppUser>()
            .Property(user => user.PasswordHash)
            .HasMaxLength(200);

        modelBuilder.Entity<AppUser>()
            .Property(user => user.Role)
            .HasMaxLength(30);

        modelBuilder.Entity<Ticket>()
            .Property(ticket => ticket.Subject)
            .HasMaxLength(120);

        modelBuilder.Entity<Ticket>()
            .Property(ticket => ticket.Text)
            .HasMaxLength(2000);

        modelBuilder.Entity<Ticket>()
            .Property(ticket => ticket.Status)
            .HasMaxLength(30);

        modelBuilder.Entity<Ticket>()
            .HasOne(ticket => ticket.CreatedBy)
            .WithMany()
            .HasForeignKey(ticket => ticket.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .HasOne(ticket => ticket.AssignedAgent)
            .WithMany()
            .HasForeignKey(ticket => ticket.AssignedAgentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(token => token.TokenHash)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .Property(token => token.TokenHash)
            .HasMaxLength(64);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(token => token.User)
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
