using System.Security.Claims;
using JwtSupportTickets.Data;
using JwtSupportTickets.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JwtSupportTickets.Controllers;

// Обрабатывает заявки с проверками владельца, роли и политики.
[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _database;

    // Получает доступ к базе данных.
    public TicketsController(AppDbContext database)
    {
        _database = database;
    }

    // Создаёт заявку от имени пользователя из JWT.
    [Authorize(Roles = "User")]
    [HttpPost]
    public async Task<ActionResult<TicketResponse>> Create(CreateTicketRequest request)
    {
        var ticket = new Ticket
        {
            Subject = request.Subject.Trim(),
            Text = request.Text.Trim(),
            Status = "Open",
            CreatedByUserId = GetCurrentUserId()
        };

        _database.Tickets.Add(ticket);
        await _database.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyTicket), new { id = ticket.Id }, ToResponse(ticket));
    }

    // Возвращает только заявки текущего пользователя.
    [Authorize(Roles = "User")]
    [HttpGet("my")]
    public async Task<ActionResult<List<TicketResponse>>> GetMyTickets()
    {
        var userId = GetCurrentUserId();
        var tickets = await _database.Tickets
            .Where(ticket => ticket.CreatedByUserId == userId)
            .OrderByDescending(ticket => ticket.Id)
            .ToListAsync();

        return Ok(tickets.Select(ToResponse).ToList());
    }

    // Возвращает одну заявку, только если она принадлежит текущему пользователю.
    [Authorize(Roles = "User")]
    [HttpGet("my/{id:int}", Name = "GetMyTicket")]
    public async Task<ActionResult<TicketResponse>> GetMyTicket(int id)
    {
        var ticket = await _database.Tickets.FindAsync(id);
        if (ticket == null)
        {
            return NotFound("Заявка не найдена.");
        }

        if (ticket.CreatedByUserId != GetCurrentUserId())
        {
            return Forbid();
        }

        return Ok(ToResponse(ticket));
    }

    // Показывает сотруднику открытые заявки без назначенного агента.
    [Authorize(Policy = "TicketsAssign")]
    [HttpGet("unassigned")]
    public async Task<ActionResult<List<TicketResponse>>> GetUnassignedTickets()
    {
        var tickets = await _database.Tickets
            .Where(ticket => ticket.Status == "Open" && ticket.AssignedAgentId == null)
            .OrderBy(ticket => ticket.Id)
            .ToListAsync();

        return Ok(tickets.Select(ToResponse).ToList());
    }

    // Назначает открытую заявку текущему агенту.
    [Authorize(Policy = "TicketsAssign")]
    [HttpPost("{id:int}/assign")]
    public async Task<ActionResult<TicketResponse>> Assign(int id)
    {
        var ticket = await _database.Tickets.FindAsync(id);
        if (ticket == null)
        {
            return NotFound("Заявка не найдена.");
        }

        var agentId = GetCurrentUserId();
        if (ticket.AssignedAgentId.HasValue && ticket.AssignedAgentId != agentId)
        {
            return Conflict("Заявка уже назначена другому сотруднику.");
        }

        if (ticket.Status != "Open")
        {
            return Conflict("Нельзя назначить закрытую заявку.");
        }

        ticket.AssignedAgentId = agentId;
        await _database.SaveChangesAsync();

        return Ok(ToResponse(ticket));
    }

    // Администратор закрывает любую заявку, агент — только назначенную ему.
    [Authorize(Roles = "Agent,Admin")]
    [HttpPost("{id:int}/close")]
    public async Task<ActionResult<TicketResponse>> Close(int id)
    {
        var ticket = await _database.Tickets.FindAsync(id);
        if (ticket == null)
        {
            return NotFound("Заявка не найдена.");
        }

        var isAdmin = User.IsInRole("Admin");
        var isAssignedAgent = ticket.AssignedAgentId == GetCurrentUserId();
        if (!isAdmin && !isAssignedAgent)
        {
            return Forbid();
        }

        if (ticket.Status == "Closed")
        {
            return Conflict("Заявка уже закрыта.");
        }

        ticket.Status = "Closed";
        await _database.SaveChangesAsync();

        return Ok(ToResponse(ticket));
    }

    // Показывает все заявки только администратору.
    [Authorize(Roles = "Admin")]
    [HttpGet("all")]
    public async Task<ActionResult<List<TicketResponse>>> GetAllTickets()
    {
        var tickets = await _database.Tickets
            .OrderByDescending(ticket => ticket.Id)
            .ToListAsync();

        return Ok(tickets.Select(ToResponse).ToList());
    }

    // Читает id пользователя из claim в JWT.
    private int GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(userId!);
    }

    // Подготавливает ответ без данных пользователя и хэша пароля.
    private static TicketResponse ToResponse(Ticket ticket)
    {
        return new TicketResponse
        {
            Id = ticket.Id,
            Subject = ticket.Subject,
            Text = ticket.Text,
            Status = ticket.Status,
            CreatedByUserId = ticket.CreatedByUserId,
            AssignedAgentId = ticket.AssignedAgentId
        };
    }
}
