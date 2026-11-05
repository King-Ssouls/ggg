using System.ComponentModel.DataAnnotations;

namespace FailConfig.Contracts;

public sealed class UploadFileRequest
{
    [Required(ErrorMessage = "Выберите файл")]
    public IFormFile? File { get; set; }
}

public sealed record StoredFileResponse(int Id, int TicketId, string OriginalName,
    string ContentType, long Size, DateTime UploadedAt);

public sealed record FileDownload(Stream Stream, string ContentType, string OriginalName);
