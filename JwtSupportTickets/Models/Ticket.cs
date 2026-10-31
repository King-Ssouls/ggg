namespace JwtSupportTickets.Models;

// Заявка хранит автора, тему, текст, статус и назначенного сотрудника.
public class Ticket
{
    public int Id { get; set; }
    public string Subject { get; set; } = "";
    public string Text { get; set; } = "";
    public string Status { get; set; } = "Open";

    public int CreatedByUserId { get; set; }
    public AppUser? CreatedBy { get; set; }

    public int? AssignedAgentId { get; set; }
    public AppUser? AssignedAgent { get; set; }
}
