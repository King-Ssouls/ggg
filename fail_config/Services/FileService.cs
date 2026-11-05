using System.Security.Claims;
using FailConfig.Contracts;
using FailConfig.Data;
using FailConfig.Extensions;
using FailConfig.Middleware;
using FailConfig.Models;
using FailConfig.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FailConfig.Services;

// Проверяет вложения и связывает файлы на диске с заявками в SQLite
public sealed class FileService(AppDbContext db, IOptions<FileUploadOptions> options,
    IWebHostEnvironment environment, ILogger<FileService> logger)
{
    private readonly FileUploadOptions fileOptions = options.Value;
    private readonly string uploadsPath = Path.Combine(environment.ContentRootPath, "storage", "uploads");

    // Сохраняет проверенный файл к своей заявке и возвращает его описание
    public async Task<StoredFileResponse> UploadAsync(int ticketId, IFormFile file,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        string fileName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        await CheckFileAsync(file, extension, cancellationToken);

        var ticket = await db.Tickets.FindAsync([ticketId], cancellationToken);
        if (ticket is null)
            throw new ApiException(404, "Заявка не найдена");
        if (ticket.AuthorId != user.GetUserId())
            throw new ApiException(403, "Загружать файлы может только автор заявки");

        DateTime now = DateTime.UtcNow;
        string safeName = Guid.NewGuid().ToString("N") + extension;
        string relativePath = Path.Combine(now.ToString("yyyy"), now.ToString("MM"),
            now.ToString("dd"), safeName);
        string filePath = Path.Combine(uploadsPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        var savedFile = new StoredFile
        {
            TicketId = ticketId,
            OriginalName = fileName,
            StoredRelativePath = relativePath,
            ContentType = GetContentType(extension),
            Size = file.Length,
            UploadedBy = user.GetUserId(),
            UploadedAt = now
        };

        bool fileCreated = false;
        try
        {
            await using (var output = new FileStream(filePath, FileMode.CreateNew,
                FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                fileCreated = true;
                await file.CopyToAsync(output, cancellationToken);
            }
            db.Files.Add(savedFile);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Если запись в базу не сохранилась, удаляем созданный файл
            try { if (fileCreated) File.Delete(filePath); }
            catch (Exception cleanupError)
            {
                logger.LogError(cleanupError, "Не удалось убрать файл после ошибки сохранения");
            }
            throw;
        }

        return ToResponse(savedFile);
    }

    // Показывает вложения автору заявки или назначенному агенту
    public async Task<List<StoredFileResponse>> GetListAsync(int ticketId,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == ticketId, cancellationToken);
        if (ticket is null)
            throw new ApiException(404, "Заявка не найдена");
        CheckReadAccess(ticket, user);
        var files = await db.Files.AsNoTracking().Where(x => x.TicketId == ticketId)
            .OrderBy(x => x.UploadedAt).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        return files.Select(ToResponse).ToList();
    }

    // Открывает файл потоком после проверки владельца и прав агента
    public async Task<FileDownload> DownloadAsync(int id, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var file = await db.Files.AsNoTracking().Include(x => x.Ticket)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (file is null)
            throw new ApiException(404, "Файл не найден");
        CheckReadAccess(file.Ticket, user);
        string path = GetFilePath(file.StoredRelativePath);
        try
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                81920, useAsync: true);
            return new FileDownload(stream, file.ContentType, file.OriginalName);
        }
        catch (FileNotFoundException) { throw new ApiException(404, "Файл отсутствует на диске"); }
        catch (DirectoryNotFoundException) { throw new ApiException(404, "Файл отсутствует на диске"); }
    }

    // Проверяет размер, белый список расширений и первые байты файла
    private async Task CheckFileAsync(IFormFile file, string extension, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            throw new ApiException(400, "Нельзя загрузить пустой файл");
        if (file.Length > fileOptions.MaxSizeBytes)
            throw new ApiException(400, $"Размер файла превышает лимит {fileOptions.MaxSizeBytes} байт");
        if (!fileOptions.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            throw new ApiException(400, "Это расширение файла не разрешено");

        byte[] firstBytes = new byte[4];
        await using var stream = file.OpenReadStream();
        int bytesRead = 0;
        while (bytesRead < firstBytes.Length)
        {
            int read = await stream.ReadAsync(firstBytes.AsMemory(bytesRead), cancellationToken);
            if (read == 0) break;
            bytesRead += read;
        }
        bool validSignature = extension switch
        {
            ".jpg" or ".jpeg" => bytesRead >= 3 && firstBytes[0] == 0xFF && firstBytes[1] == 0xD8 && firstBytes[2] == 0xFF,
            ".png" => bytesRead >= 4 && firstBytes[0] == 0x89 && firstBytes[1] == 0x50 && firstBytes[2] == 0x4E && firstBytes[3] == 0x47,
            ".pdf" => bytesRead >= 4 && firstBytes[0] == 0x25 && firstBytes[1] == 0x50 && firstBytes[2] == 0x44 && firstBytes[3] == 0x46,
            _ => false
        };
        if (!validSignature)
            throw new ApiException(400, "Содержимое файла не совпадает с расширением");
    }

    // Разрешает чтение автору или агенту, которому назначена заявка
    private static void CheckReadAccess(Ticket ticket, ClaimsPrincipal user)
    {
        int userId = user.GetUserId();
        if (ticket.AuthorId != userId && !(user.IsInRole("Agent") && ticket.AgentId == userId))
            throw new ApiException(403, "Нет доступа к файлам этой заявки");
    }

    // Не позволяет сохранённому пути выйти из папки загрузок
    private string GetFilePath(string relativePath)
    {
        string root = Path.GetFullPath(uploadsPath) + Path.DirectorySeparatorChar;
        string path = Path.GetFullPath(Path.Combine(uploadsPath, relativePath));
        if (Path.IsPathRooted(relativePath) || !path.StartsWith(root, StringComparison.Ordinal))
            throw new ApiException(404, "Файл не найден");
        return path;
    }

    // MIME задаёт сервер после проверки расширения и сигнатуры
    private static string GetContentType(string extension) => extension switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream"
    };

    // Возвращает клиенту метаданные без внутреннего пути и имени на диске
    private static StoredFileResponse ToResponse(StoredFile file) => new(file.Id, file.TicketId,
        file.OriginalName, file.ContentType, file.Size, file.UploadedAt);
}
