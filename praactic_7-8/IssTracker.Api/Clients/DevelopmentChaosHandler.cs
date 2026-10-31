using System.Net;
using System.Text;
using IssTracker.Api.Options;
using IssTracker.Api.Services;
using Microsoft.Extensions.Options;

namespace IssTracker.Api.Clients;

// Только для разработки: имитирует ошибки внешнего API.
public class DevelopmentChaosHandler : DelegatingHandler
{
    private readonly FailureSimulationState _simulation;
    private readonly int _timeoutSeconds;
    private readonly ILogger<DevelopmentChaosHandler> _logger;

    // Получает режим симуляции, таймаут и журнал.
    public DevelopmentChaosHandler(
        FailureSimulationState simulation,
        IOptions<IssApiOptions> options,
        ILogger<DevelopmentChaosHandler> logger)
    {
        _simulation = simulation;
        _timeoutSeconds = options.Value.RequestTimeoutSeconds;
        _logger = logger;
    }

    // Выполняет выбранную симуляцию или передаёт запрос дальше.
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var (mode, requestNumber) = _simulation.NextRequest();

        if (mode == FailureMode.FailTwice && requestNumber <= 2)
        {
            _logger.LogWarning("Учебная симуляция вернула HTTP 503, запрос {RequestNumber}", requestNumber);
            return CreateUnavailableResponse(request);
        }

        if (mode == FailureMode.AlwaysFail)
        {
            _logger.LogWarning("Учебная симуляция вернула постоянный HTTP 503");
            return CreateUnavailableResponse(request);
        }

        if (mode == FailureMode.Slow)
        {
            _logger.LogWarning("Учебная симуляция задерживает внешний API");
            await Task.Delay(TimeSpan.FromSeconds(_timeoutSeconds + 2), cancellationToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    // Создаёт простой ответ 503 без обращения к настоящему API.
    private static HttpResponseMessage CreateUnavailableResponse(HttpRequestMessage request)
    {
        return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            RequestMessage = request,
            Content = new StringContent(
                "Искусственная ошибка для учебной проверки.",
                Encoding.UTF8,
                "text/plain")
        };
    }
}
