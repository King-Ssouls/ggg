using Microsoft.EntityFrameworkCore;
using ConcertTicketsApi.Contracts;
using ConcertTicketsApi.Data;

namespace ConcertTicketsApi.Services;

public sealed class ConcertReportService(AppDbContext db)
{
    public Task<List<SectorRevenueDto>> GetSectorRevenueAsync(int eventId, CancellationToken cancellationToken)
    {
        // Interpolated values are sent as SQLite parameters by EF Core, never concatenated into SQL.
        return db.Database.SqlQuery<SectorRevenueDto>($"""
            SELECT
                s.Id AS SectorId,
                s.Name AS SectorName,
                s.Capacity AS Capacity,
                s.SoldCount AS SoldCount,
                (s.Capacity - s.SoldCount) AS RemainingSeats,
                COALESCE(SUM(CASE WHEN t.Status = 'Sold' THEN t.SoldPrice ELSE 0 END), 0) AS Revenue
            FROM ConcertEvents e
            INNER JOIN Sectors s ON s.ConcertEventId = e.Id
            LEFT JOIN Tickets t ON t.SectorId = s.Id
            WHERE e.Id = {eventId}
            GROUP BY s.Id, s.Name, s.Capacity, s.SoldCount
            ORDER BY s.Name, s.Id
            """).ToListAsync(cancellationToken);
    }
}
