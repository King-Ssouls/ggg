using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConcertTicketsApi.Data;

namespace ConcertTicketsApi.Controllers;

[ApiController]
[Route("api/diagnostics")]
public sealed class DiagnosticsController(AppDbContext db) : ControllerBase
{
    [HttpGet("no-tracking-experiment/{eventId:int}")]
    public async Task<ActionResult<object>> NoTrackingExperiment(
        int eventId,
        [FromQuery] string proposedTitle = "Это изменение не должно сохраниться",
        CancellationToken cancellationToken = default)
    {
        var detached = await db.ConcertEvents.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == eventId, cancellationToken);
        if (detached is null)
        {
            return NotFound(ApiProblem.Create(HttpContext, 404, "Событие не найдено", "Проверьте идентификатор события."));
        }

        var originalTitle = detached.Title;
        detached.Title = proposedTitle.Length <= 160 ? proposedTitle : proposedTitle[..160];
        var stateBeforeSave = db.Entry(detached).State.ToString();
        await db.SaveChangesAsync(cancellationToken);

        var storedTitle = await db.ConcertEvents.AsNoTracking()
            .Where(x => x.Id == eventId).Select(x => x.Title).SingleAsync(cancellationToken);
        return Ok(new
        {
            originalTitle,
            proposedTitle = detached.Title,
            stateBeforeSave,
            storedTitleAfterSaveChanges = storedTitle,
            conclusion = "AsNoTracking вернул отсоединенный объект: SaveChanges не сформировал UPDATE."
        });
    }
}
