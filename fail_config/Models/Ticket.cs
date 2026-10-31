namespace FailConfig.Models;

// Заявка принадлежит автору и может быть назначена одному агенту поддержки
public sealed class Ticket
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int AuthorId { get; set; }
    public AppUser Author { get; set; } = null!;
    public int? AgentId { get; set; }
    public AppUser? Agent { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<StoredFile> Files { get; set; } = new List<StoredFile>();
}
