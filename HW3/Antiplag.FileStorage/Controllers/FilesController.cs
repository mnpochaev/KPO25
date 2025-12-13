using Antiplag.FileStorage.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Antiplag.FileStorage.Controllers;

[ApiController]
[Route("internal/files")]
public class FilesController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IWebHostEnvironment env, ILogger<FilesController> logger)
    {
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Принимает файл и сохраняет его на диск.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<StoreFileResponse>> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is required");

        var storageDir = Path.Combine(_env.ContentRootPath, "files");
        Directory.CreateDirectory(storageDir);

        var id = Guid.NewGuid();

        // сохраняем GUID + расширение, чтобы потом можно было восстановить тип
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext))
            ext = string.Empty;

        var savedName = id + ext;
        var fullPath = Path.Combine(storageDir, savedName);

        try
        {
            await using var stream = System.IO.File.Create(fullPath);
            await file.CopyToAsync(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save file");
            return StatusCode(500, "Failed to save file");
        }

        var resp = new StoreFileResponse
        {
            FileId = id,
            OriginalName = file.FileName,
            ContentType = file.ContentType ?? "application/octet-stream",
            SizeBytes = file.Length,
            UploadedAt = DateTime.UtcNow
        };

        return Ok(resp);
    }

    /// <summary>
    /// Возвращает бинарное содержимое файла по его ID + выставляет корректные заголовки.
    /// </summary>
    [HttpGet("{fileId:guid}")]
    public IActionResult GetFile(Guid fileId)
    {
        var storageDir = Path.Combine(_env.ContentRootPath, "files");
        if (!Directory.Exists(storageDir))
            return NotFound();

        var filePath = Directory
            .GetFiles(storageDir)
            .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == fileId.ToString());

        if (filePath is null)
            return NotFound();

        // 1) правильный Content-Type по расширению
        var provider = new FileExtensionContentTypeProvider();

        // На всякий случай добавим маппинги, если вдруг чего-то нет
        provider.Mappings.TryAdd(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        provider.Mappings.TryAdd(".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation");
        provider.Mappings.TryAdd(".pdf", "application/pdf");
        provider.Mappings.TryAdd(".txt", "text/plain; charset=utf-8");

        if (!provider.TryGetContentType(filePath, out var contentType))
            contentType = "application/octet-stream";

        // 2) Content-Disposition с именем файла (чтобы Analysis видел расширение)
        var downloadName = Path.GetFileName(filePath); // например: {guid}.pdf

        return PhysicalFile(
            filePath,
            contentType,
            fileDownloadName: downloadName,
            enableRangeProcessing: true
        );
    }
}