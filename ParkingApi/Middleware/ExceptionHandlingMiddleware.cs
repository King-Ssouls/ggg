using Microsoft.AspNetCore.Mvc;

namespace ParkingApi.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Внутренняя ошибка сервера",
                Detail = "Произошла внутренняя ошибка. Повторите запрос позже.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                Instance = context.Request.Path
            };
            problem.Extensions["correlationId"] = context.GetCorrelationId();

            await context.Response.WriteAsJsonAsync(
                problem,
                contentType: "application/problem+json",
                cancellationToken: context.RequestAborted);
        }
    }
}
