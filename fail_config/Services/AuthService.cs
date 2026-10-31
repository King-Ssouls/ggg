using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FailConfig.Contracts;
using FailConfig.Data;
using FailConfig.Models;
using FailConfig.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FailConfig.Services;

// Сервис сохраняет хеш пароля и создаёт JWT после успешного входа
public sealed class AuthService(
    AppDbContext db,
    IPasswordHasher<AppUser> passwordHasher,
    IOptions<JwtOptions> jwtOptions)
{
    // Регистрация всегда создаёт обычного пользователя без права выбирать роль
    public async Task<AppUser?> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken)
    {
        var name = dto.UserName.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.UserName == name, cancellationToken))
        {
            return null;
        }

        var user = new AppUser { UserName = name, Role = "User" };
        user.PasswordHash = passwordHasher.HashPassword(user, dto.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    // Верный пароль даёт токен с идентификатором и ролью пользователя
    public async Task<LoginResponse?> LoginAsync(LoginDto dto, CancellationToken cancellationToken)
    {
        var name = dto.UserName.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.UserName == name, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var check = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (check == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (check == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, dto.Password);
            await db.SaveChangesAsync(cancellationToken);
        }

        var settings = jwtOptions.Value;
        var expiresAt = DateTime.UtcNow.AddMinutes(settings.AccessMinutes);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            new UserResponse(user.Id, user.UserName, user.Role));
    }
}
