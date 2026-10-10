using Microsoft.AspNetCore.Mvc;
using ConcertTicketsApi.Contracts;
using ConcertTicketsApi.Services;

namespace ConcertTicketsApi.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController(ConcertReportService reports) : ControllerBase
{
    [HttpGet("events/{eventId:int}/sector-revenue")]
    public async Task<ActionResult<IReadOnlyList<SectorRevenueDto>>> SectorRevenue(int eventId, CancellationToken cancellationToken)
    {
        var rows = await reports.GetSectorRevenueAsync(eventId, cancellationToken);
        return Ok(rows);
    }
}
