namespace FailConfig.Options;

// Хранит настройки выпуска и проверки JWT
public sealed class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public int AccessMinutes { get; set; }
}
