using System.Diagnostics;

namespace ParkingApi.Middleware;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        catch
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }

            throw;
        }
        finally
        {
            timer.Stop();
            logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                timer.Elapsed.TotalMilliseconds);
        }
    }
}
