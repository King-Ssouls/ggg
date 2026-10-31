using FailConfig.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FailConfig.Data;

// Учебный агент создаётся из внешних настроек без пароля в исходном коде
public static class SeedData
{
    // Пустой пароль отключает создание агента, поэтому его можно добавить при настройке User Secrets
    public static async Task EnsureAgentAsync(
        AppDbContext db,
        IPasswordHasher<AppUser> passwordHasher,
        IConfiguration configuration)
    {
        var password = configuration["SeedAgent:Password"];
        if (string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (password.Length < 8 || password.Length > 100)
        {
            throw new InvalidOperationException("Пароль учебного агента должен содержать от 8 до 100 символов");
        }

        var name = configuration["SeedAgent:UserName"]?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "agent";
        }

        var existingUser = await db.Users.SingleOrDefaultAsync(x => x.UserName == name);
        if (existingUser is not null)
        {
            if (existingUser.Role != "Agent")
            {
                throw new InvalidOperationException("Имя учебного агента уже занято обычным пользователем");
            }

            return;
        }

        var user = new AppUser { UserName = name, Role = "Agent" };
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
}
