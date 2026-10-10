using Microsoft.AspNetCore.Mvc;

namespace ParkingApi.Controllers;

internal static class ApiProblemDetails
{
    public static ProblemDetails Create(HttpContext context, int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Instance = context.Request.Path,
        Extensions = { ["correlationId"] = context.GetCorrelationId() }
    };
}
