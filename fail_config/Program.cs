using System.Security.Claims;
using System.Text;
using FailConfig.Data;
using FailConfig.Middleware;
using FailConfig.Models;
using FailConfig.Options;
using FailConfig.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Для учебного API все сообщения выводятся в обычную консоль
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole();

// Настройки читаются один раз через классы и проверяются при запуске
builder.Services.AddOptions<FileUploadOptions>()
    .Bind(builder.Configuration.GetSection("FileUpload"))
    .Validate(x => x.MaxSizeBytes > 0 && x.MaxSizeBytes <= 104857600,
        "Лимит файла должен быть от 1 байта до 100 МБ")
    .Validate(x => x.AllowedExtensions is { Length: > 0 } && x.AllowedExtensions.All(e =>
        new[] { ".jpg", ".jpeg", ".png", ".pdf" }.Contains(e, StringComparer.OrdinalIgnoreCase)),
        "Укажите разрешённые расширения jpg, jpeg, png или pdf")
    .ValidateOnStart();
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .Validate(x => !string.IsNullOrWhiteSpace(x.Issuer), "Укажите Jwt:Issuer")
    .Validate(x => !string.IsNullOrWhiteSpace(x.Audience), "Укажите Jwt:Audience")
    .Validate(x => !string.IsNullOrWhiteSpace(x.Secret) && x.Secret.Length >= 32,
        "Jwt:Secret должен содержать не меньше 32 символов — настройте User Secrets или Jwt__Secret")
    .Validate(x => x.AccessMinutes > 0 && x.AccessMinutes <= 1440,
        "Срок JWT должен быть от 1 до 1440 минут")
    .ValidateOnStart();

// HTTP-форма может быть чуть больше файла из-за её служебных полей
builder.Services.AddOptions<KestrelServerOptions>().Configure<IOptions<FileUploadOptions>>(
    (server, files) => server.Limits.MaxRequestBodySize = files.Value.MaxSizeBytes + 262144);
builder.Services.AddOptions<FormOptions>().Configure<IOptions<FileUploadOptions>>(
    (form, files) => form.MultipartBodyLengthLimit = files.Value.MaxSizeBytes + 262144);

builder.Services.AddDbContext<AppDbContext>(db =>
    db.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<FileService>();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// Выпуск и проверка подписи JWT используют один JwtOptions
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, options) =>
    {
        var settings = options.Value;
        bearer.MapInboundClaims = false;
        bearer.IncludeErrorDetails = false;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };
        bearer.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (!int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out int id) || id <= 0)
                    context.Fail("В токене нет идентификатора пользователя");
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// Кнопка Authorize в Swagger передаёт JWT в защищённые методы
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(swagger =>
{
    swagger.SwaggerDoc("v1", new OpenApiInfo { Title = "Заявки поддержки — практика 6", Version = "v1" });
    swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Вставьте только значение token из ответа login"
    });
    swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = []
    });
});

var app = builder.Build();

// Неверные настройки останавливают приложение до создания базы и файлов
try
{
    _ = app.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
    _ = app.Services.GetRequiredService<IOptions<FileUploadOptions>>().Value;
}
catch (OptionsValidationException error)
{
    foreach (string message in error.Failures)
        Console.Error.WriteLine(message);
    Environment.ExitCode = 1;
    return;
}

app.UseMiddleware<ErrorMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Миграция создаёт таблицы, а агент добавляется только при наличии внешнего пароля
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.EnsureAgentAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>(),
        app.Configuration);
}

app.Run();
