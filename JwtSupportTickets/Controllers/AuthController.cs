using JwtSupportTickets.Data;
using JwtSupportTickets.Models;
using JwtSupportTickets.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JwtSupportTickets.Controllers;

// Регистрирует пользователей, выполняет вход и обновляет токены.
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _database;
    private readonly TokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    // Получает базу, сервис токенов и журнал приложения.
    public AuthController(
        AppDbContext database,
        TokenService tokenService,
        ILogger<AuthController> logger)
    {
        _database = database;
        _tokenService = tokenService;
        _logger = logger;
    }

    // Создаёт обычного пользователя. Роль через регистрацию не передаётся.
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<TokenResponse>> Register(RegisterRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var emailIsUsed = await _database.Users.AnyAsync(user => user.Email == email);

        if (emailIsUsed)
        {
            return Conflict("Пользователь с таким email уже существует.");
        }

        var user = new AppUser
        {
            Email = email,
            // В базе сохраняется хэш пароля, а не исходный пароль.
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "User"
        };

        _database.Users.Add(user);
        await _database.SaveChangesAsync();

        return Ok(await CreateTokenResponse(user));
    }

    // Проверяет email и пароль и выдаёт пару токенов.
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request)
    {
        var email = NormalizeEmail(request.Email);
        _logger.LogInformation("Попытка входа для {Email}", email);

        var user = await _database.Users.SingleOrDefaultAsync(item => item.Email == email);
        // BCrypt сравнивает введённый пароль с сохранённым хэшем.
        var passwordIsCorrect = user != null
            && BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!passwordIsCorrect)
        {
            return Unauthorized("Неверный логин или пароль.");
        }

        return Ok(await CreateTokenResponse(user!));
    }

    // Меняет использованный refresh-токен на новую пару токенов.
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> Refresh(RefreshRequest request)
    {
        // Ищем строку по хэшу, чтобы не хранить refresh-токен открытым текстом.
        var tokenHash = TokenService.HashRefreshToken(request.RefreshToken);
        var savedToken = await _database.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash);

        if (savedToken == null || savedToken.User == null)
        {
            return Unauthorized("Недействительный refresh-токен.");
        }

        var now = DateTime.UtcNow;
        if (savedToken.RevokedAt.HasValue)
        {
            // Повтор старого токена завершает все активные refresh-сессии пользователя.
            var activeTokens = await _database.RefreshTokens
                .Where(token => token.UserId == savedToken.UserId && token.RevokedAt == null)
                .ToListAsync();

            foreach (var activeToken in activeTokens)
            {
                activeToken.RevokedAt = now;
            }

            await _database.SaveChangesAsync();
            return Unauthorized("Refresh-токен уже использован. Войдите снова.");
        }

        if (savedToken.ExpiresAt <= now)
        {
            return Unauthorized("Срок действия refresh-токена истёк.");
        }

        savedToken.RevokedAt = now;
        return Ok(await CreateTokenResponse(savedToken.User));
    }

    // Сохраняет хэш refresh-токена и возвращает исходный токен клиенту.
    private async Task<TokenResponse> CreateTokenResponse(AppUser user)
    {
        var (accessToken, expiresAt) = _tokenService.CreateAccessToken(user);
        var refreshToken = _tokenService.CreateRefreshToken();
        var now = DateTime.UtcNow;

        _database.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = TokenService.HashRefreshToken(refreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(7)
        });

        await _database.SaveChangesAsync();

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAtUtc = expiresAt
        };
    }

    // Приводит email к единому виду перед поиском и сохранением.
    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
