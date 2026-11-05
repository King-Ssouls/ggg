using FailConfig.Contracts;
using FailConfig.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FailConfig.Controllers;

// Принимает вложения заявок и отдаёт их только после проверки JWT и прав
[ApiController]
[Authorize]
public sealed class FilesController(FileService files) : ControllerBase
{
    // Принимает файл из multipart-формы и добавляет к своей заявке
    [HttpPost("api/tickets/{ticketId:int}/files")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<StoredFileResponse>> Upload(int ticketId,
        [FromForm] UploadFileRequest form, CancellationToken cancellationToken)
    {
        var result = await files.UploadAsync(ticketId, form.File!, User, cancellationToken);
        return CreatedAtAction(nameof(Download), new { id = result.Id }, result);
    }

    // Показывает список вложений одной доступной заявки
    [HttpGet("api/tickets/{ticketId:int}/files")]
    public async Task<ActionResult<List<StoredFileResponse>>> GetList
    (int ticketId,
        CancellationToken
        cancellationToken
    )
        => Ok(await files.GetListAsync
        (
            ticketId,
            User,
            cancellationToken)
        );

    // Возвращает поток с исходным именем для скачивания
    [HttpGet("api/files/{id:int}")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var result = await files.DownloadAsync(id, User, cancellationToken);
        return File(result.Stream, result.ContentType, result.OriginalName, enableRangeProcessing: true);
    }
}
