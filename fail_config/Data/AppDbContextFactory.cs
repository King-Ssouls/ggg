using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FailConfig.Data;

// Даёт инструменту миграций доступ к базе без запуска JWT и HTTP-сервера
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    // Читает строку подключения из настроек при выполнении dotnet ef
    public AppDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
        var options = new DbContextOptionsBuilder<AppDbContext>();
        options.UseSqlite(config.GetConnectionString("Default"));
        return new AppDbContext(options.Options);
    }
}
