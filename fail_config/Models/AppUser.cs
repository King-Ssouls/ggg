namespace FailConfig.Models;

// Пользователь хранит имя, хеш пароля и роль для проверки доступа
public sealed class AppUser
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}
