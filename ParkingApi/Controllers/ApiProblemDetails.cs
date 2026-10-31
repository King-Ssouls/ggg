using Microsoft.AspNetCore.Mvc;
using ParkingApi.Middleware;

namespace ParkingApi.Controllers;

internal static class ApiProblemDetails
{
    // Единый формат объясняет клиенту ошибку и возвращает идентификатор запроса для поиска в логах
    public static ProblemDetails Create(HttpContext context, int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Instance = context.Request.Path,
        Extensions = { ["correlationId"] = context.GetCorrelationId() }
    };
}
