using FailConfig.Contracts;
using FailConfig.Data;
using FailConfig.Extensions;
using FailConfig.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FailConfig.Controllers;

// Контроллер показывает автору его заявки, а агенту только назначенные ему
[ApiController]
[Authorize]
[Route("api/tickets")]
public sealed class TicketsController(AppDbContext db) : ControllerBase
{
    // Возвращает список заявок, которые доступны текущему пользователю
    [HttpGet]
    public async Task<ActionResult<List<TicketResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var isAgent = User.IsInRole("Agent");
        var tickets = await db.Tickets.AsNoTracking()
            .Where(x => x.AuthorId == userId || (isAgent && x.AgentId == userId))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new TicketResponse(x.Id, x.Title, x.Text, x.AuthorId, x.AgentId, x.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(tickets);
    }

    // Читает одну заявку после проверки автора или назначенного агента
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (ticket is null)
        {
            return Problem(statusCode: 404, title: "Заявка не найдена");
        }

        if (!CanRead(ticket))
        {
            return Problem(statusCode: 403, title: "Нет доступа к этой заявке");
        }

        return Ok(ToResponse(ticket));
    }

    // Создаёт заявку с автором из JWT, поэтому подставить чужой ID через JSON нельзя
    [HttpPost]
    public async Task<ActionResult<TicketResponse>> Create(CreateTicketDto dto, CancellationToken cancellationToken)
    {
        var ticket = new Ticket
        {
            Title = dto.Title.Trim(),
            Text = dto.Text.Trim(),
            AuthorId = User.GetUserId(),
            CreatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ToResponse(ticket));
    }

    // Даёт автору список сотрудников, которым можно назначить заявку
    [HttpGet("agents")]
    public async Task<ActionResult<List<AgentResponse>>> GetAgents(CancellationToken cancellationToken)
    {
        var agents = await db.Users.AsNoTracking().Where(x => x.Role == "Agent")
            .OrderBy(x => x.UserName)
            .Select(x => new AgentResponse(x.Id, x.UserName))
            .ToListAsync(cancellationToken);
        return Ok(agents);
    }

    // Только автор может назначить существующего агента своей заявке
    [HttpPut("{id:int}/agent")]
    public async Task<ActionResult<TicketResponse>> AssignAgent(
        int id, AssignAgentDto dto, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (ticket is null)
        {
            return Problem(statusCode: 404, title: "Заявка не найдена");
        }

        if (ticket.AuthorId != User.GetUserId())
        {
            return Problem(statusCode: 403, title: "Назначить агента может только автор заявки");
        }

        var agentExists = await db.Users.AnyAsync(x => x.Id == dto.AgentId && x.Role == "Agent", cancellationToken);
        if (!agentExists)
        {
            return Problem(statusCode: 404, title: "Агент не найден");
        }

        ticket.AgentId = dto.AgentId;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(ticket));
    }

    // Агент получает доступ к чужой заявке только после назначения на неё
    private bool CanRead(Ticket ticket)
    {
        var userId = User.GetUserId();
        return ticket.AuthorId == userId || (User.IsInRole("Agent") && ticket.AgentId == userId);
    }

    // DTO ответа отделяет публичные поля от связанных объектов базы
    private static TicketResponse ToResponse(Ticket ticket) =>
        new(ticket.Id, ticket.Title, ticket.Text, ticket.AuthorId, ticket.AgentId, ticket.CreatedAt);
}
