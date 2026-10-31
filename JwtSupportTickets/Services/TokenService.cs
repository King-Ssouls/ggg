using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JwtSupportTickets.Models;
using Microsoft.IdentityModel.Tokens;

namespace JwtSupportTickets.Services;

// Выпускает access-токены и создаёт значения refresh-токенов.
public class TokenService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;

    // Читает секрет и параметры JWT из конфигурации.
    public TokenService(IConfiguration configuration)
    {
        _secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Не задан Jwt:Secret.");
        _issuer = configuration["Jwt:Issuer"] ?? "JwtSupportTickets";
        _audience = configuration["Jwt:Audience"] ?? "JwtSupportTicketsClient";
    }

    // Создаёт JWT со сроком действия 15 минут.
    public (string Token, DateTime ExpiresAtUtc) CreateAccessToken(AppUser user)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(15);
        // Эти claims позволяют API узнать id, email, роль и право пользователя.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        if (user.Role == "Agent")
        {
            claims.Add(new Claim("permission", "tickets.assign"));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        // Создаёт подписанный токен; его содержимое не шифруется.
        var jwt = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expiresAt);
    }

    // Создаёт случайный refresh-токен для клиента.
    public string CreateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    // Преобразует refresh-токен в хэш для хранения в базе.
    public static string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }
}
