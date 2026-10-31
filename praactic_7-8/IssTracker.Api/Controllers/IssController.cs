using IssTracker.Api.Data;
using IssTracker.Api.Models;
using IssTracker.Api.Options;
using IssTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IssTracker.Api.Controllers;

// Даёт доступ к последнему положению МКС, истории и синхронизации.
[ApiController]
[Route("api/iss")]
public class IssController : ControllerBase
{
    private readonly IssDbContext _database;
    private readonly IssSyncService _syncService;
    private readonly IssSyncState _syncState;
    private readonly int _maxAgeMinutes;

    // Получает базу, синхронизацию и настройки свежести данных.
    public IssController(
        IssDbContext database,
        IssSyncService syncService,
        IssSyncState syncState,
        IOptions<IssApiOptions> options)
    {
        _database = database;
        _syncService = syncService;
        _syncState = syncState;
        _maxAgeMinutes = options.Value.MaxDataAgeMinutes;
    }

    // Возвращает последнее измерение и сообщает, не устарело ли оно.
    [HttpGet("latest")]
    public async Task<ActionResult<IssLatestResponse>> GetLatest(CancellationToken cancellationToken)
    {
        var position = await _database.Positions
            .OrderByDescending(item => item.CapturedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (position == null)
        {
            return StatusCode(503, new { message = "Положение МКС пока не сохранено." });
        }

        var capturedAtUtc = DateTime.SpecifyKind(position.CapturedAtUtc, DateTimeKind.Utc);
        var ageSeconds = Math.Max(0, (DateTime.UtcNow - capturedAtUtc).TotalSeconds);
        return Ok(new IssLatestResponse
        {
            Position = ToView(position),
            AgeSeconds = ageSeconds,
            IsStale = ageSeconds > TimeSpan.FromMinutes(_maxAgeMinutes).TotalSeconds
        });
    }

    // Возвращает историю, где limit всегда находится в диапазоне от 1 до 500.
    [HttpGet("history")]
    public async Task<ActionResult<List<IssPositionView>>> GetHistory(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 500);
        var positions = await _database.Positions
            .OrderByDescending(item => item.CapturedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return Ok(positions.Select(ToView).ToList());
    }

    // Показывает последнюю попытку, последний успех и число ошибок подряд.
    [HttpGet("status")]
    public ActionResult<IssStatusResponse> GetStatus()
    {
        return Ok(_syncState.GetSnapshot());
    }

    // Запускает ручное обновление тем же сервисом, что использует worker.
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var result = await _syncService.RunOnceAsync(cancellationToken);

        if (result.Outcome == SyncOutcome.Busy)
        {
            return Conflict(new { message = "Синхронизация уже выполняется. Попробуйте позже." });
        }

        if (result.Outcome == SyncOutcome.Failed)
        {
            return StatusCode(503, new
            {
                message = "Не удалось обновить данные. Последнее сохранённое измерение оставлено в базе.",
                error = result.ErrorMessage
            });
        }

        return Ok(new
        {
            message = result.SavedNewPosition
                ? "Новое положение МКС сохранено."
                : "Это измерение уже было сохранено.",
            capturedAtUtc = result.CapturedAtUtc
        });
    }

    // Возвращает линию GeoJSON от старой точки к новой.
    [HttpGet("track.geojson")]
    public async Task<IActionResult> GetTrackGeoJson(
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 2, 500);
        var newestFirst = await _database.Positions
            .OrderByDescending(item => item.CapturedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (newestFirst.Count < 2)
        {
            return StatusCode(503, new { message = "Для траектории нужно хотя бы два измерения." });
        }

        var points = newestFirst
            .OrderBy(item => item.CapturedAtUtc)
            .ToList();

        // В GeoJSON порядок каждой точки такой: долгота, затем широта.
        var coordinates = points
            .Select(item => new[] { item.Longitude, item.Latitude })
            .ToArray();

        return Ok(new
        {
            type = "Feature",
            geometry = new
            {
                type = "LineString",
                coordinates
            },
            properties = new
            {
                pointCount = points.Count,
                firstCapturedAtUtc = DateTime.SpecifyKind(points[0].CapturedAtUtc, DateTimeKind.Utc),
                lastCapturedAtUtc = DateTime.SpecifyKind(points[^1].CapturedAtUtc, DateTimeKind.Utc)
            }
        });
    }

    // Отдаёт клиенту только поля измерения, которые нужны для показа.
    private static IssPositionView ToView(IssPosition position)
    {
        return new IssPositionView
        {
            Id = position.Id,
            SatelliteId = position.SatelliteId,
            Name = position.Name,
            Latitude = position.Latitude,
            Longitude = position.Longitude,
            AltitudeKm = position.AltitudeKm,
            VelocityKph = position.VelocityKph,
            Visibility = position.Visibility,
            CapturedAtUtc = DateTime.SpecifyKind(position.CapturedAtUtc, DateTimeKind.Utc),
            SavedAtUtc = DateTime.SpecifyKind(position.SavedAtUtc, DateTimeKind.Utc)
        };
    }
}
