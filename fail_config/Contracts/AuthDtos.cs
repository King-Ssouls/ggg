using System.ComponentModel.DataAnnotations;

namespace FailConfig.Contracts;

// Клиент передаёт только имя и пароль, роль назначает сервер
public sealed class RegisterDto
{
    [Required(ErrorMessage = "Укажите имя пользователя")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Имя должно содержать от 3 до 50 символов")]
    [RegularExpression("^[A-Za-z0-9_]+$", ErrorMessage = "Используйте латинские буквы, цифры и знак подчёркивания")]
    public string UserName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Укажите пароль")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Пароль должен содержать от 8 до 100 символов")]
    public string Password { get; init; } = string.Empty;
}

// Данные входа нужны для проверки пароля и выдачи JWT
public sealed class LoginDto
{
    [Required(ErrorMessage = "Укажите имя пользователя")]
    [StringLength(50)]
    public string UserName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Укажите пароль")]
    [StringLength(100)]
    public string Password { get; init; } = string.Empty;
}

// Ответ не содержит пароль или его хеш
public sealed record UserResponse(int Id, string UserName, string Role);

// Клиент получает токен, время его окончания и сведения о пользователе
public sealed record LoginResponse(string Token, DateTime ExpiresAt, UserResponse User);
