using System.ComponentModel.DataAnnotations;

namespace FailConfig.Contracts;

// Принимает файл из формы Swagger с полем file
public sealed class UploadFileRequest
{
    [Required(ErrorMessage = "Выберите файл")]
    public IFormFile? File { get; set; }
}

// Показывает сведения о файле без пути на диске
public sealed record StoredFileResponse(int Id, int TicketId, string OriginalName,
    string ContentType, long Size, DateTime UploadedAt);

// Передаёт открытый поток контроллеру для скачивания
public sealed record FileDownload(Stream Stream, string ContentType, string OriginalName);
