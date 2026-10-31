using Microsoft.AspNetCore.Mvc;

namespace FailConfig.Middleware;

// Возвращает ошибки одним JSON-форматом и скрывает внутренние подробности сбоя
public sealed class ErrorMiddleware(RequestDelegate next, ILogger<ErrorMiddleware> logger)
{
    // Передаёт запрос дальше и перехватывает ошибки сервисов
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception error) when (!context.Response.HasStarted)
        {
            int status = error switch
            {
                ApiException known => known.Status,
                BadHttpRequestException badRequest => badRequest.StatusCode,
                _ => StatusCodes.Status500InternalServerError
            };

            string message = error is ApiException ? error.Message
                : status == 413 ? "Размер HTTP-запроса превышает лимит сервера"
                : status < 500 ? "Проверьте формат запроса"
                : "Внутренняя ошибка сервера";

            if (status >= 500)
                logger.LogError(error, "Ошибка запроса {TraceId}", context.TraceIdentifier);

            context.Response.Clear();
            context.Response.StatusCode = status;
            var problem = new ProblemDetails
            {
                Status = status,
                Title = message,
                Instance = context.Request.Path
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;
            await context.Response.WriteAsJsonAsync(problem, options: null,
                contentType: "application/problem+json", cancellationToken: context.RequestAborted);
        }
    }
}
