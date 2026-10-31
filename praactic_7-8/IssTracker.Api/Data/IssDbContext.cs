using IssTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IssTracker.Api.Data;

// Описывает таблицы и индексы базы SQLite.
public class IssDbContext : DbContext
{
    // Получает параметры подключения от контейнера приложения.
    public IssDbContext(DbContextOptions<IssDbContext> options) : base(options)
    {
    }

    public DbSet<IssPosition> Positions => Set<IssPosition>();

    // Не даёт сохранить два измерения с одним временем съёмки.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IssPosition>(entity =>
        {
            entity.HasIndex(position => position.CapturedAtUtc).IsUnique();
            entity.Property(position => position.Name).HasMaxLength(100);
            entity.Property(position => position.Visibility).HasMaxLength(30);
        });
    }
}
