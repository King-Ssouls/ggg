using FailConfig.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FailConfig.Controllers;

// Показывает настройки, которые безопасно открыть без токена
[ApiController]
[Route("api/config")]
public sealed class ConfigController(
        IWebHostEnvironment environment,
        IOptions<FileUploadOptions> files,
        IOptions<JwtOptions> jwt
    ) : ControllerBase
{
    // Возвращает среду и лимиты, но только длину секретного ключа
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        environment = environment.EnvironmentName,
        maxSizeBytes = files.Value.MaxSizeBytes,
        allowedExtensions = files.Value.AllowedExtensions,
        secretLength = jwt.Value.Secret.Length
    });
}
