namespace ParkingApi.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";
    private const string ItemName = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        // Сохраняет входной идентификатор или создаёт новый, затем передаёт его в ответ и лог
        var supplied = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = string.IsNullOrWhiteSpace(supplied) ? Guid.NewGuid().ToString() : supplied;
        context.Items[ItemName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope("CorrelationId: {CorrelationId}", correlationId))
        {
            await next(context);
        }
    }
}

public static class HttpContextCorrelationExtensions
{
    // Контроллеры и обработчики ошибок получают тот же идентификатор из контекста запроса
    public static string GetCorrelationId(this HttpContext context) =>
        context.Items.TryGetValue("CorrelationId", out var value) && value is string id
            ? id
            : string.Empty;
}
