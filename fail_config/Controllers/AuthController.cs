using FailConfig.Contracts;
using FailConfig.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FailConfig.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register(RegisterDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var user = await authService.RegisterAsync(dto, cancellationToken);
            if (user is null)
            {
                return Problem(statusCode: 409, title: "Имя пользователя уже занято");
            }

            return StatusCode(201, new UserResponse(user.Id, user.UserName, user.Role));
        }
        catch (DbUpdateException error) when (
            error.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
        {
            return Problem(statusCode: 409, title: "Имя пользователя уже занято");
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginDto dto, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(dto, cancellationToken);
        if (result is null)
        {
            return Problem(statusCode: 401, title: "Неверное имя пользователя или пароль");
        }

        return Ok(result);
    }
}
