using System.Net.Http.Headers;
using System.Net.Http.Json;
using Antiplag.Gateway.Models;
using Microsoft.AspNetCore.Mvc;

namespace Antiplag.Gateway.Controllers;

[ApiController]
[Route("api/v1/submissions")]
public class SubmissionsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SubmissionsController> _logger;

    public SubmissionsController(
        IHttpClientFactory httpClientFactory,
        ILogger<SubmissionsController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Внешний эндпоинт для студента:
    /// принимает файл и метаданные, сохраняет файл и запускает анализ.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<CreateSubmissionResponse>> CreateSubmission(
        [FromForm] CreateSubmissionForm form)
    {
        if (form.File == null || form.File.Length == 0)
            return BadRequest("File is required");

        var storageClient = _httpClientFactory.CreateClient("fileStorage");
        var analysisClient = _httpClientFactory.CreateClient("analysis");

        var submissionId = Guid.NewGuid();

        // ---------- 1) Отправляем файл в FileStorage ----------
        Guid fileId;

        using (var content = new MultipartFormDataContent())
        {
            var fileContent = new StreamContent(form.File.OpenReadStream());
            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue(form.File.ContentType ?? "application/octet-stream");

            content.Add(fileContent, "file", form.File.FileName);

            HttpResponseMessage storageResponse;
            try
            {
                storageResponse = await storageClient.PostAsync("/internal/files", content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling FileStorage");
                return StatusCode(503, "FileStorage service unavailable");
            }

            if (!storageResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("FileStorage returned {Status}", storageResponse.StatusCode);
                return StatusCode((int)storageResponse.StatusCode, "Could not store file");
            }

            var storePayload = await storageResponse.Content.ReadFromJsonAsync<StoreFileResponseProxy>();
            if (storePayload == null)
            {
                return StatusCode(500, "Invalid response from FileStorage");
            }

            fileId = storePayload.FileId;
        }

        // ---------- 2) Запускаем анализ в Analysis ----------
        var analysisRequest = new AnalysisRequestProxy
        {
            SubmissionId = submissionId,
            FileId = fileId,
            WorkId = form.WorkId,
            StudentId = form.StudentId
        };

        HttpResponseMessage analysisResponse;
        try
        {
            analysisResponse = await analysisClient.PostAsJsonAsync(
                "/internal/analysis/submissions",
                analysisRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Analysis");
            return StatusCode(503, "Analysis service unavailable");
        }

        if (!analysisResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Analysis returned {Status}", analysisResponse.StatusCode);
            return StatusCode((int)analysisResponse.StatusCode, "Could not analyze submission");
        }

        var report = await analysisResponse.Content.ReadFromJsonAsync<AnalysisReportProxy>();
        if (report == null)
        {
            return StatusCode(500, "Invalid response from Analysis");
        }

        // ---------- 3) Формируем ответ студенту ----------
        var result = new CreateSubmissionResponse
        {
            SubmissionId = submissionId,
            FileId = fileId,
            WorkId = form.WorkId,
            StudentId = form.StudentId,
            StudentName = form.StudentName,
            Group = form.Group,
            Plagiarism = report.Plagiarism,
            Similarity = report.Similarity,
            OriginalSubmissionId = report.OriginalSubmissionId,
            OriginalStudentId = report.OriginalStudentId,
            WordCloudUrl = report.WordCloudUrl
        };

        return Ok(result);
    }

    // Внутренний DTO для ответа FileStorage
    private class StoreFileResponseProxy
    {
        public Guid FileId { get; set; }
        public string OriginalName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long SizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    // Внутренний DTO для ответа Analysis
    private class AnalysisReportProxy
    {
        public Guid ReportId { get; set; }
        public Guid SubmissionId { get; set; }
        public bool Plagiarism { get; set; }
        public double Similarity { get; set; }
        public Guid? OriginalSubmissionId { get; set; }
        public string? OriginalStudentId { get; set; }
        public string? WordCloudUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class AnalysisRequestProxy
    {
        public Guid SubmissionId { get; set; }
        public Guid FileId { get; set; }
        public string WorkId { get; set; } = null!;
        public string StudentId { get; set; } = null!;
    }
}
