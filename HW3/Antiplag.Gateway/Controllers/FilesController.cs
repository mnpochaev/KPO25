using Microsoft.AspNetCore.Mvc;

namespace Antiplag.Gateway.Controllers;

[ApiController]
[Route("api/v1/files")]
[Tags("Files")]
public class FilesController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        IHttpClientFactory httpClientFactory,
        ILogger<FilesController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Скачивание файла по его идентификатору.
    /// Преподаватель (или студент) может открыть или сохранить оригинальный файл работы.
    /// </summary>
    [HttpGet("{fileId:guid}")]
    public async Task<IActionResult> Download(Guid fileId)
    {
        var client = _httpClientFactory.CreateClient("fileStorage");

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync($"/internal/files/{fileId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling FileStorage for file {FileId}", fileId);
            return StatusCode(503, "FileStorage service unavailable");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("FileStorage returned {Status} for file {FileId}",
                response.StatusCode, fileId);

            return StatusCode((int)response.StatusCode, "Could not download file");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var contentType = response.Content.Headers.ContentType?.ToString()
                          ?? "application/octet-stream";

        var disposition = response.Content.Headers.ContentDisposition;
        var fileName = disposition?.FileNameStar
                       ?? disposition?.FileName
                       ?? $"{fileId}";

        // убираем кавычки у FileName, если есть
        fileName = fileName.Trim('"');

        return File(bytes, contentType, fileName);
    }
}
