using System.Diagnostics;
using IssTracker.Api.Clients;
using IssTracker.Api.Data;
using IssTracker.Api.Metrics;
using IssTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IssTracker.Api.Services;

// Выполняет один полный цикл получения и сохранения положения МКС.
public class IssSyncService
{
    private readonly IssApiClient _apiClient;
    private readonly IssDbContext _database;
    private readonly IssSyncState _state;
    private readonly SyncLock _syncLock;
    private readonly ILogger<IssSyncService> _logger;

    // Получает клиент внешнего API, базу, состояние и общий замок синхронизации.
    public IssSyncService(
        IssApiClient apiClient,
        IssDbContext database,
        IssSyncState state,
        SyncLock syncLock,
        ILogger<IssSyncService> logger)
    {
        _apiClient = apiClient;
        _database = database;
        _state = state;
        _syncLock = syncLock;
        _logger = logger;
    }

    // Получает измерение и сохраняет его, если время ещё не встречалось.
    public async Task<SyncRunResult> RunOnceAsync(CancellationToken cancellationToken)
    {
        if (!_syncLock.TryEnter())
        {
            _logger.LogInformation("Синхронизация пропущена: другая попытка ещё выполняется");
            return new SyncRunResult { Outcome = SyncOutcome.Busy };
        }

        var timer = Stopwatch.StartNew();
        try
        {
            IssMetrics.SyncAttempts.Inc();
            _state.MarkAttempt(DateTimeOffset.UtcNow);

            var data = await _apiClient.GetCurrentPositionAsync(cancellationToken);
            var capturedAt = DateTimeOffset.FromUnixTimeSeconds(data.Timestamp).UtcDateTime;

            // Один timestamp — одна запись, даже если API вернул тот же ответ повторно.
            var exists = await _database.Positions
                .AnyAsync(position => position.CapturedAtUtc == capturedAt, cancellationToken);

            if (!exists)
            {
                var position = new IssPosition
                {
                    SatelliteId = data.Id,
                    Name = data.Name ?? "ISS",
                    Latitude = data.Latitude,
                    Longitude = data.Longitude,
                    AltitudeKm = data.AltitudeKm,
                    VelocityKph = data.VelocityKph,
                    Visibility = data.Visibility ?? "Unknown",
                    CapturedAtUtc = capturedAt,
                    SavedAtUtc = DateTime.UtcNow
                };

                _database.Positions.Add(position);
                await _database.SaveChangesAsync(cancellationToken);
            }

            _state.MarkSuccess(DateTimeOffset.UtcNow);
            _logger.LogInformation(
                "ISS position updated: {Latitude}, {Longitude}; captured at {CapturedAtUtc}; saved new record: {SavedNew}; duration {DurationMs} ms",
                data.Latitude,
                data.Longitude,
                capturedAt,
                !exists,
                timer.ElapsedMilliseconds);

            return new SyncRunResult
            {
                Outcome = SyncOutcome.Succeeded,
                CapturedAtUtc = new DateTimeOffset(capturedAt, TimeSpan.Zero),
                SavedNewPosition = !exists
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // При остановке приложения отмена должна завершать работу, а не считаться ошибкой API.
            throw;
        }
        catch (Exception exception)
        {
            _state.MarkFailure(exception.Message);
            IssMetrics.SyncFailures.Inc();
            _logger.LogError(exception, "ISS synchronization failed after {DurationMs} ms", timer.ElapsedMilliseconds);

            return new SyncRunResult
            {
                Outcome = SyncOutcome.Failed,
                ErrorMessage = exception.Message
            };
        }
        finally
        {
            timer.Stop();
            IssMetrics.SyncDuration.Observe(timer.Elapsed.TotalSeconds);
            _syncLock.Exit();
        }
    }
}
