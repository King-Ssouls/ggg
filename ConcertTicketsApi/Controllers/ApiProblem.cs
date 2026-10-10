using Microsoft.AspNetCore.Mvc;

namespace ConcertTicketsApi.Controllers;

internal static class ApiProblem
{
    public static ProblemDetails Create(HttpContext context, int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Instance = context.Request.Path
    };
}
