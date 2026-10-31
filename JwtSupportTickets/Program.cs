using System.Security.Claims;
using System.Text;
using JwtSupportTickets.Data;
using JwtSupportTickets.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// Настраивает базу, проверку JWT, политику доступа и Swagger.
var builder = WebApplication.CreateBuilder(args);

// Секрет читается из конфигурации и не хранится в исходном коде.
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException(
        "Добавьте Jwt:Secret в User Secrets. Секрет должен быть не короче 32 символов.");
}

var issuer = builder.Configuration["Jwt:Issuer"] ?? "JwtSupportTickets";
var audience = builder.Configuration["Jwt:Audience"] ?? "JwtSupportTicketsClient";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

// Подключает SQLite и сервис, который выпускает токены.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<TokenService>();
builder.Services.AddControllers();

// Настраивает проверку подписи, срока, издателя и получателя JWT.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// Агент должен иметь специальный claim, чтобы назначать заявки.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("TicketsAssign", policy =>
        policy.RequireAuthenticatedUser()
            .RequireClaim("permission", "tickets.assign"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Вставьте access-токен из ответа метода login.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = Array.Empty<string>()
    });
});

var app = builder.Build();

// Создаёт файл базы данных и таблицы при первом запуске.
using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await database.Database.EnsureCreatedAsync();
}

app.UseSwagger();
app.UseSwaggerUI();
// Сначала определяется пользователь по JWT, затем проверяются его права.
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
