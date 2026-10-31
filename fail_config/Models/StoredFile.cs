namespace FailConfig.Models;

// Описывает файл в базе, сами байты хранятся на диске
public sealed class StoredFile
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public string OriginalName { get; set; } = string.Empty;
    public string StoredRelativePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public int UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
}
