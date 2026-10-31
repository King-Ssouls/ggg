using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingApi.Data;
using ParkingApi.Filters;
using ParkingApi.Middleware;
using ParkingApi.Models;
using ParkingApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Здесь настраиваются сервисы, которыми пользуются контроллеры и промежуточные обработчики
builder.Logging.AddSimpleConsole(options => options.IncludeScopes = true);
builder.Services.AddControllers(options =>
{
    // Проверка FluentValidation запускается до автоматического ответа ApiController
    options.Filters.Add<FluentValidationActionFilter>();
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Проверьте заполнение полей",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
        };
        problem.Extensions["correlationId"] = context.HttpContext.GetCorrelationId();

        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});
builder.Services.AddProblemDetails(options =>
{
    // Идентификатор запроса добавляется в стандартные ответы об ошибках
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.GetCorrelationId();
});
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<ParkingBookingService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Обработчики связывают запрос с логами, записывают его результат и формируют ответ при сбое
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRouting();
app.UseCors("Frontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// При запуске схема SQLite обновляется по миграциям из папки Migrations
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
