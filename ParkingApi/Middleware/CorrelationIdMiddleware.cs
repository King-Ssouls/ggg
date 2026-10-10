namespace ParkingApi.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";
    private const string ItemName = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = string.IsNullOrWhiteSpace(supplied) ? Guid.NewGuid().ToString() : supplied;
        context.Items[ItemName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}

public static class HttpContextCorrelationExtensions
{
    public static string GetCorrelationId(this HttpContext context) =>
        context.Items.TryGetValue("CorrelationId", out var value) && value is string id
            ? id
            : string.Empty;
}
