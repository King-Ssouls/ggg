using FailConfig.Models;
using Microsoft.EntityFrameworkCore;

namespace FailConfig.Data;

// Контекст связывает пользователей, заявки и описания файлов с таблицами SQLite
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<StoredFile> Files => Set<StoredFile>();

    // Здесь задаются обязательные поля, индексы и связи таблиц
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(user =>
        {
            user.Property(x => x.UserName).IsRequired().HasMaxLength(50);
            user.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
            user.Property(x => x.Role).IsRequired().HasMaxLength(20);
            user.HasIndex(x => x.UserName).IsUnique();
        });

        modelBuilder.Entity<Ticket>(ticket =>
        {
            ticket.Property(x => x.Title).IsRequired().HasMaxLength(150);
            ticket.Property(x => x.Text).IsRequired().HasMaxLength(5000);
            ticket.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
            ticket.HasOne(x => x.Agent).WithMany().HasForeignKey(x => x.AgentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StoredFile>(file =>
        {
            file.Property(x => x.OriginalName).IsRequired().HasMaxLength(255);
            file.Property(x => x.StoredRelativePath).IsRequired().HasMaxLength(300);
            file.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
            file.HasOne(x => x.Ticket).WithMany(x => x.Files).HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Restrict);
            file.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);
            file.HasIndex(x => x.StoredRelativePath).IsUnique();
            file.HasIndex(x => new { x.TicketId, x.UploadedAt });
        });
    }
}
