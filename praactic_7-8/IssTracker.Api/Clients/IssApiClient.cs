using System.Net.Http.Json;
using System.Text.Json;
using IssTracker.Api.Models;
using IssTracker.Api.Options;
using Microsoft.Extensions.Options;

namespace IssTracker.Api.Clients;

// Отправляет HTTP-запросы сервису Where the ISS at и проверяет ответ.
public class IssApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IssApiOptions _options;

    // Получает готовый HttpClient от фабрики и настройки из конфигурации.
    public IssApiClient(HttpClient httpClient, IOptions<IssApiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    // Запрашивает и проверяет текущее положение МКС.
    public async Task<IssApiResponse> GetCurrentPositionAsync(CancellationToken cancellationToken)
    {
        var requestPath = $"satellites/{_options.SatelliteId}?units=kilometers";
        using var response = await _httpClient.GetAsync(
            requestPath,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        IssApiResponse? result;
        try
        {
            result = await response.Content.ReadFromJsonAsync<IssApiResponse>(
                cancellationToken: cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new IssApiException("Внешний API вернул некорректный JSON.", exception);
        }

        if (result == null)
        {
            throw new IssApiException("Внешний API вернул пустой ответ.");
        }

        // Проверяет обязательные диапазоны из задания.
        if (result.Latitude < -90 || result.Latitude > 90
            || result.Longitude < -180 || result.Longitude > 180
            || result.Timestamp <= 0 || result.Timestamp > 253402300799)
        {
            throw new IssApiException("Внешний API вернул данные вне допустимого диапазона.");
        }

        return result;
    }
}
