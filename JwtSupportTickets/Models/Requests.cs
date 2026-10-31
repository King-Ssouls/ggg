using System.ComponentModel.DataAnnotations;

namespace JwtSupportTickets.Models;

// Данные для создания учётной записи.
public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = "";

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = "";
}

// Данные для входа в систему.
public class LoginRequest
{
    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = "";

    [Required, MaxLength(100)]
    public string Password { get; set; } = "";
}

// Запрос на получение новой пары токенов.
public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = "";
}

// Данные новой заявки от пользователя.
public class CreateTicketRequest
{
    [Required, StringLength(120, MinimumLength = 3)]
    public string Subject { get; set; } = "";

    [Required, StringLength(2000, MinimumLength = 5)]
    public string Text { get; set; } = "";
}

// Ответ с токенами после входа или обновления.
public class TokenResponse
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
}

// Безопасное представление заявки для API.
public class TicketResponse
{
    public int Id { get; set; }
    public string Subject { get; set; } = "";
    public string Text { get; set; } = "";
    public string Status { get; set; } = "";
    public int CreatedByUserId { get; set; }
    public int? AssignedAgentId { get; set; }
}
