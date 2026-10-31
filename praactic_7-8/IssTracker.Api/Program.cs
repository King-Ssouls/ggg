using IssTracker.Api.Clients;
using IssTracker.Api.Data;
using IssTracker.Api.Health;
using IssTracker.Api.Options;
using IssTracker.Api.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Polly;
using Prometheus;
using Serilog;
using Serilog.Events;

// Собирает настройки и зависимости приложения.
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<IssApiOptions>()
    .Bind(builder.Configuration.GetSection(IssApiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<IssDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<IssSyncState>();
builder.Services.AddSingleton<SyncLock>();
builder.Services.AddScoped<IssSyncService>();
builder.Services.AddHostedService<IssTrackingWorker>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Настраивает структурированные логи в консоль и ежедневные файлы.
builder.Host.UseSerilog((context, logger) => logger
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Extensions.Http.Resilience", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/iss-tracker-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7));

builder.Services.AddHealthChecks()
    .AddCheck("live", () => HealthCheckResult.Healthy("Процесс работает."), tags: new[] { "live" })
    .AddCheck<IssReadinessHealthCheck>("iss-data", tags: new[] { "ready" });

builder.Services.AddSwaggerGen();

var isDevelopment = builder.Environment.IsDevelopment();
if (isDevelopment)
{
    // Регистрирует искусственные ошибки только в режиме разработки.
    builder.Services.AddSingleton<FailureSimulationState>();
    builder.Services.AddTransient<DevelopmentChaosHandler>();
}

var requestTimeoutSeconds = builder.Configuration.GetValue<int>(
    $"{IssApiOptions.SectionName}:RequestTimeoutSeconds");

var issClientBuilder = builder.Services.AddHttpClient<IssApiClient, IssApiClient>((services, client) =>
{
    var options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<IssApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    // Таймаутом управляет resilience handler, а не общий таймаут HttpClient.
    client.Timeout = Timeout.InfiniteTimeSpan;
});

// Polly повторяет временные ошибки, ограничивает время и открывает circuit breaker.
issClientBuilder.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromMilliseconds(500);
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.Retry.UseJitter = true;
    options.Retry.OnRetry = retry =>
    {
        Log.Warning(
            "OnRetry: попытка {AttemptNumber}, пауза {RetryDelay}",
            retry.AttemptNumber,
            retry.RetryDelay);
        return ValueTask.CompletedTask;
    };

    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(requestTimeoutSeconds);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);

    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.MinimumThroughput = 4;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
});

if (isDevelopment)
{
    // Симулятор расположен внутри resilience handler, поэтому Polly видит искусственные 503.
    issClientBuilder.AddHttpMessageHandler<DevelopmentChaosHandler>();
}

var app = builder.Build();

// Применяет InitialCreate при первом запуске и обновляет базу по миграциям.
await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<IssDbContext>();
    await database.Database.MigrateAsync();
}

app.UseSerilogRequestLogging();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseHttpMetrics();
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapMetrics();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResultStatusCodes =
    {
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
        [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable
    }
});

if (app.Environment.IsDevelopment())
{
    // Этот маршрут отсутствует вне режима разработки.
    app.MapPost("/api/debug/failure-mode/{mode}", (
        string mode,
        FailureSimulationState simulation) =>
    {
        if (!Enum.TryParse<FailureMode>(mode, ignoreCase: true, out var selectedMode)
            || !Enum.IsDefined(typeof(FailureMode), selectedMode))
        {
            return Results.BadRequest(new
            {
                message = "Используйте один из режимов: None, FailTwice, AlwaysFail или Slow."
            });
        }

        simulation.SetMode(selectedMode);
        return Results.Ok(new { mode = selectedMode.ToString() });
    });
}

await app.RunAsync();
