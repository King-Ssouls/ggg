using Microsoft.EntityFrameworkCore;
using ConcertTicketsApi.Contracts;
using ConcertTicketsApi.Data;
using ConcertTicketsApi.Models;

namespace ConcertTicketsApi.Services;

public enum TicketCommandFailure
{
    None,
    Conflict,
    NotFound
}

public sealed record TicketCommandResult(Ticket? Ticket, TicketCommandFailure Failure, string? Error, int RemainingSeats)
{
    public static TicketCommandResult Success(Ticket ticket, int remaining) =>
        new(ticket, TicketCommandFailure.None, null, remaining);

    public static TicketCommandResult Conflict(string detail) =>
        new(null, TicketCommandFailure.Conflict, detail, 0);

    public static TicketCommandResult Missing(string detail) =>
        new(null, TicketCommandFailure.NotFound, detail, 0);
}

public sealed class TicketSalesService(AppDbContext db)
{
    public async Task<TicketCommandResult> SellAsync(SellTicketDto dto, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var sector = await db.Sectors.Include(x => x.ConcertEvent)
            .SingleOrDefaultAsync(x => x.Id == dto.SectorId, cancellationToken);
        if (sector is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return TicketCommandResult.Missing("Сектор не найден.");
        }

        if (sector.ConcertEvent.Status == ConcertEventStatus.Cancelled)
        {
            await transaction.RollbackAsync(cancellationToken);
            return TicketCommandResult.Conflict("Нельзя продавать билеты на отмененное событие.");
        }

        if (sector.SoldCount >= sector.Capacity)
        {
            await transaction.RollbackAsync(cancellationToken);
            return TicketCommandResult.Conflict("В секторе больше нет свободных мест.");
        }

        var category = await db.PriceCategories.SingleOrDefaultAsync(
            x => x.Id == dto.PriceCategoryId && x.SectorId == sector.Id, cancellationToken);
        if (category is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return TicketCommandResult.Conflict("Ценовая категория не относится к выбранному сектору.");
        }

        var buyer = await db.Buyers.SingleOrDefaultAsync(x => x.Id == dto.BuyerId, cancellationToken);
        if (buyer is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return TicketCommandResult.Conflict("Покупатель не найден.");
        }

        var ticket = new Ticket
        {
            Number = $"TCK-{Guid.NewGuid():N}",
            Status = TicketStatus.Sold,
            SoldAt = DateTime.Now,
            SoldPrice = category.Price,
            InternalBarcode = Guid.NewGuid().ToString("N"),
            Sector = sector,
            PriceCategory = category,
            Buyer = buyer
        };
        sector.SoldCount++;
        sector.Version = Guid.NewGuid();
        db.Tickets.Add(ticket);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return TicketCommandResult.Success(ticket, sector.Capacity - sector.SoldCount);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return TicketCommandResult.Conflict("Сектор был изменен другой кассой. Обновите данные и повторите продажу.");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return TicketCommandResult.Conflict("Билет не сохранен из-за конфликта данных. Проверьте сектор и повторите запрос.");
        }
    }

    public async Task<TicketCommandResult> RefundAsync(int ticketId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var ticket = await db.Tickets.Include(x => x.Sector).ThenInclude(x => x.ConcertEvent)
            .Include(x => x.Buyer)
            .Include(x => x.PriceCategory)
            .SingleOrDefaultAsync(x => x.Id == ticketId, cancellationToken);
        if (ticket is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return TicketCommandResult.Missing("Билет не найден.");
        }

        if (ticket.Status != TicketStatus.Sold)
        {
            await transaction.RollbackAsync(cancellationToken);
            return TicketCommandResult.Conflict("Этот билет уже возвращен.");
        }

        if (ticket.Sector.ConcertEvent.StartsAt <= DateTime.Now)
        {
            // The attempted return is explicitly rolled back; the ticket and sector remain unchanged.
            await transaction.RollbackAsync(cancellationToken);
            return TicketCommandResult.Conflict("Возврат возможен только до начала события.");
        }

        ticket.Status = TicketStatus.Refunded;
        ticket.Sector.SoldCount--;
        ticket.Sector.Version = Guid.NewGuid();
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return TicketCommandResult.Success(ticket, ticket.Sector.Capacity - ticket.Sector.SoldCount);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return TicketCommandResult.Conflict("Сектор был изменен другой кассой. Обновите данные и повторите возврат.");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return TicketCommandResult.Conflict("Возврат не сохранен из-за конфликта данных.");
        }
    }
}
