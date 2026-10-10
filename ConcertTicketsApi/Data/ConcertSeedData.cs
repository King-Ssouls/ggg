using Microsoft.EntityFrameworkCore;
using ConcertTicketsApi.Models;

namespace ConcertTicketsApi.Data;

public static class ConcertSeedData
{
    public static async Task EnsureSeededAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.ConcertEvents.AnyAsync(cancellationToken))
        {
            return;
        }

        var venue = new Venue { Name = "Зал Север", Address = "Москва, ул. Музыкальная, 15" };
        var sponsor = new Sponsor { Name = "Маяк" };
        var buyers = new[]
        {
            new Buyer { LastName = "Иванова", FirstName = "Анна", Email = "anna@example.test" },
            new Buyer { LastName = "Петров", FirstName = "Илья", Email = "ilya@example.test" },
            new Buyer { LastName = "Соколова", FirstName = "Мария", Email = "maria@example.test" }
        };
        db.Venues.Add(venue);
        db.Sponsors.Add(sponsor);
        db.Buyers.AddRange(buyers);

        var now = DateTime.Now;
        for (var i = 0; i < 45; i++)
        {
            var concert = new ConcertEvent
            {
                Title = $"Концертная программа {i + 1:00}",
                StartsAt = i == 44 ? now.Date.AddDays(-1).AddHours(19) : now.Date.AddDays(i + 1).AddHours(19),
                Status = i == 14 ? ConcertEventStatus.Cancelled : ConcertEventStatus.Scheduled,
                Venue = venue,
                Sponsor = i % 3 == 0 ? sponsor : null
            };
            var sector = new Sector
            {
                Name = "Партер",
                Capacity = 20,
                SoldCount = i == 0 ? 2 : i == 44 ? 1 : 0,
                Version = Guid.NewGuid(),
                ConcertEvent = concert
            };
            var standard = new PriceCategory { Name = "Стандарт", Price = 2500m, Sector = sector };
            sector.PriceCategories.Add(standard);
            sector.PriceCategories.Add(new PriceCategory { Name = "Льготный", Price = 1500m, Sector = sector });
            concert.Sectors.Add(sector);
            db.ConcertEvents.Add(concert);

            if (i == 0)
            {
                db.Tickets.AddRange(
                    MakeSeedTicket("TCK-SEED-0001", 2500m, sector, standard, buyers[0]),
                    MakeSeedTicket("TCK-SEED-0002", 2500m, sector, standard, buyers[1]));
            }
            else if (i == 44)
            {
                db.Tickets.Add(MakeSeedTicket("TCK-SEED-PAST-0001", 2500m, sector, standard, buyers[2]));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Ticket MakeSeedTicket(string number, decimal price, Sector sector, PriceCategory category, Buyer buyer) => new()
    {
        Number = number,
        Status = TicketStatus.Sold,
        SoldAt = DateTime.Now,
        SoldPrice = price,
        InternalBarcode = Guid.NewGuid().ToString("N"),
        Sector = sector,
        PriceCategory = category,
        Buyer = buyer
    };
}
