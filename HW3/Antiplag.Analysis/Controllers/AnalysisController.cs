using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Antiplag.Analysis.Data;
using Antiplag.Analysis.Data.Entities;
using Antiplag.Analysis.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace Antiplag.Analysis.Controllers;

[ApiController]
[Route("internal/analysis")]
[Tags("Analysis")]
public class AnalysisController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AnalysisDbContext _db;
    private readonly ILogger<AnalysisController> _logger;

    // Настройки алгоритма
    private const int ShingleLength = 5;      // размер шингла (5 слов)
    private const double Threshold = 0.5;     // порог плагиата (50%)

    public AnalysisController(
        IHttpClientFactory httpClientFactory,
        AnalysisDbContext db,
        ILogger<AnalysisController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Анализ одной сдачи: скачивает файл, считает шинглы и сравнивает с прошлым.
    /// Факт сдачи и результат анализа сохраняются в Postgres.
    /// </summary>
    [HttpPost("submissions")]
    public async Task<ActionResult<ReportDto>> AnalyzeSubmission(
        [FromBody] SubmissionAnalysisRequest request)
    {
        if (request.SubmissionId == Guid.Empty || request.FileId == Guid.Empty)
            return BadRequest("SubmissionId and FileId are required");

        if (string.IsNullOrWhiteSpace(request.WorkId) || string.IsNullOrWhiteSpace(request.StudentId))
            return BadRequest("WorkId and StudentId are required");

        // защита от повторного запроса (idempotency-ish)
        if (await _db.Submissions.AsNoTracking().AnyAsync(s => s.SubmissionId == request.SubmissionId))
            return Conflict("Submission already exists");

        // ---------- 1. Скачиваем файл из FileStorage ----------
        var client = _httpClientFactory.CreateClient("fileStorage");

        HttpResponseMessage fileResponse;
        try
        {
            fileResponse = await client.GetAsync($"/internal/files/{request.FileId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call FileStorage");
            return StatusCode(503, "FileStorage service unavailable");
        }

        if (!fileResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("FileStorage returned {Status} for file {FileId}",
                fileResponse.StatusCode, request.FileId);

            return StatusCode((int)fileResponse.StatusCode,
                "Cannot get file from storage");
        }

        var fileBytes = await fileResponse.Content.ReadAsByteArrayAsync();

        // ---------- 2. Считаем хэш и шинглы + готовим текст ----------
        var hash = ComputeSha256(fileBytes);

        var contentType = fileResponse.Content.Headers.ContentType?.MediaType
                          ?? "application/octet-stream";

        var disposition = fileResponse.Content.Headers.ContentDisposition;
        var fileName = disposition?.FileNameStar ?? disposition?.FileName;
        fileName = fileName?.Trim('"');

        // Вытаскиваем текст (PDF / DOCX / PPTX / TXT)
        var rawText = ExtractText(fileBytes, contentType, fileName);

        // Текст для антиплагиата
        var textForPlagiarism = rawText;

        // Текст для облака слов (очищенный и обрезанный)
        var cloudText = SanitizeForWordCloud(rawText);

        // Шинглы по нормальному тексту
        var shingles = BuildShingles(textForPlagiarism, ShingleLength);

        // Ссылка на wordcloud (PNG, не SVG)
        string? wordCloudUrl = null;
        if (!string.IsNullOrWhiteSpace(cloudText))
        {
            wordCloudUrl =
                "https://quickchart.io/wordcloud?format=png&width=600&height=600&text=" +
                Uri.EscapeDataString(cloudText);
        }

        // ---------- 3. Ищем совпадения с уже существующими работами ----------
        var now = DateTime.UtcNow;

        List<(Guid SubmissionId, string StudentId, string Hash, string[] Shingles, DateTime CreatedAt)> snapshot;
        try
        {
            snapshot = await _db.Submissions
                .AsNoTracking()
                .Where(s => s.WorkId == request.WorkId && s.StudentId != request.StudentId)
                .OrderBy(s => s.CreatedAt)
                .Select(s => new ValueTuple<Guid, string, string, string[], DateTime>(
                    s.SubmissionId,
                    s.StudentId,
                    s.Hash,
                    s.Shingles,
                    s.CreatedAt))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query Postgres");
            return StatusCode(503, "Database unavailable");
        }

        // 3.1. Полное совпадение по хэшу (берём самый ранний)
        var exactOriginal = snapshot.FirstOrDefault(s => s.Hash == hash);
        var hasExact = exactOriginal.SubmissionId != Guid.Empty;

        // 3.2. Частичное совпадение по шинглам
        Guid? bestMatchSubmissionId = null;
        string? bestMatchStudentId = null;
        double bestSimilarity = 0.0;

        foreach (var prev in snapshot)
        {
            var sim = ComputeJaccardSimilarity(shingles, prev.Shingles);
            if (sim > bestSimilarity)
            {
                bestSimilarity = sim;
                bestMatchSubmissionId = prev.SubmissionId;
                bestMatchStudentId = prev.StudentId;
            }
        }

        // ---------- 4. Формируем отчёт ----------
        var reportId = Guid.NewGuid();

        bool plagiarism;
        double similarity;
        Guid? originalSubmissionId;
        string? originalStudentId;

        if (hasExact)
        {
            plagiarism = true;
            similarity = 1.0;
            originalSubmissionId = exactOriginal.SubmissionId;
            originalStudentId = exactOriginal.StudentId;
        }
        else if (bestMatchSubmissionId is not null && bestSimilarity >= Threshold)
        {
            plagiarism = true;
            similarity = bestSimilarity;
            originalSubmissionId = bestMatchSubmissionId;
            originalStudentId = bestMatchStudentId;
        }
        else
        {
            plagiarism = false;
            similarity = bestSimilarity;
            originalSubmissionId = null;
            originalStudentId = null;
        }

        var reportDto = new ReportDto
        {
            ReportId = reportId,
            SubmissionId = request.SubmissionId,
            CreatedAt = now,
            Plagiarism = plagiarism,
            Similarity = similarity,
            OriginalSubmissionId = originalSubmissionId,
            OriginalStudentId = originalStudentId,
            WordCloudUrl = wordCloudUrl
        };

        // ---------- 5. Сохраняем сдачу + отчёт в Postgres ----------
        var submissionEntity = new SubmissionEntity
        {
            SubmissionId = request.SubmissionId,
            WorkId = request.WorkId,
            StudentId = request.StudentId,
            FileId = request.FileId,
            Hash = hash,
            Shingles = shingles.ToArray(),
            CreatedAt = now
        };

        var reportEntity = new ReportEntity
        {
            ReportId = reportId,
            SubmissionId = request.SubmissionId,
            Plagiarism = plagiarism,
            Similarity = similarity,
            OriginalSubmissionId = originalSubmissionId,
            OriginalStudentId = originalStudentId,
            WordCloudUrl = wordCloudUrl,
            CreatedAt = now
        };

        try
        {
            _db.Submissions.Add(submissionEntity);
            _db.Reports.Add(reportEntity);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to save submission/report to Postgres");
            return StatusCode(503, "Database unavailable");
        }

        return Ok(reportDto);
    }

    /// <summary>
    /// Возвращает список отчётов по всем сдачам конкретной работы (для преподавателя).
    /// </summary>
    [HttpGet("works/{workId}/reports")]
    public async Task<ActionResult<WorkReportSummaryDto>> GetReportsForWork(string workId)
    {
        if (string.IsNullOrWhiteSpace(workId))
            return BadRequest("workId is required");

        List<WorkReportItemDto> items;

        try
        {
            items = await _db.Reports
                .AsNoTracking()
                .Where(r => r.Submission.WorkId == workId)
                .OrderBy(r => r.CreatedAt)
                .Select(r => new WorkReportItemDto
                {
                    SubmissionId = r.SubmissionId,
                    StudentId = r.Submission.StudentId,
                    FileId = r.Submission.FileId,
                    Plagiarism = r.Plagiarism,
                    Similarity = r.Similarity,
                    OriginalSubmissionId = r.OriginalSubmissionId,
                    OriginalStudentId = r.OriginalStudentId,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query reports from Postgres");
            return StatusCode(503, "Database unavailable");
        }

        var summary = new WorkReportSummaryDto
        {
            WorkId = workId,
            Items = items
        };

        if (items.Count == 0)
        {
            summary.TotalSubmissions = 0;
            summary.PlagiarizedCount = 0;
            summary.PlagiarizedShare = 0;
            summary.AverageSimilarity = 0;
            summary.MaxSimilarity = 0;
            summary.UniqueStudents = 0;
            summary.FirstSubmissionAt = null;
            summary.LastSubmissionAt = null;

            return Ok(summary);
        }

        summary.TotalSubmissions = items.Count;
        summary.PlagiarizedCount = items.Count(r => r.Plagiarism);
        summary.PlagiarizedShare = (double)summary.PlagiarizedCount / summary.TotalSubmissions;
        summary.AverageSimilarity = items.Average(r => r.Similarity);
        summary.MaxSimilarity = items.Max(r => r.Similarity);
        summary.UniqueStudents = items.Select(r => r.StudentId).Distinct().Count();
        summary.FirstSubmissionAt = items.Min(r => r.CreatedAt);
        summary.LastSubmissionAt = items.Max(r => r.CreatedAt);

        return Ok(summary);
    }

    // ---------- Вспомогательные методы ----------

    /// <summary>
    /// Извлечение текста из файла в зависимости от формата.
    /// Поддерживаются: PDF, DOCX, PPTX, текстовые файлы.
    /// </summary>
    private static string ExtractText(byte[] fileBytes, string contentType, string? fileName)
    {
        try
        {
            // 1) PDF по сигнатуре "%PDF"
            if (LooksLikePdf(fileBytes))
            {
                using var ms = new MemoryStream(fileBytes);
                using var pdf = PdfDocument.Open(ms);
                var sb = new StringBuilder();
                foreach (var page in pdf.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
                return sb.ToString();
            }

            // 2) OOXML (DOCX/PPTX) по сигнатуре ZIP ("PK")
            if (LooksLikeZip(fileBytes))
            {
                // Пытаемся как DOCX
                try
                {
                    using var ms = new MemoryStream(fileBytes);
                    using var doc = WordprocessingDocument.Open(ms, false);
                    var txt = doc.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(txt))
                        return txt;
                }
                catch
                {
                    // ignore, попробуем PPTX
                }

                // Пытаемся как PPTX
                try
                {
                    using var ms = new MemoryStream(fileBytes);
                    using var pres = PresentationDocument.Open(ms, false);
                    var sb = new StringBuilder();
                    var pptPart = pres.PresentationPart;
                    if (pptPart != null)
                    {
                        foreach (var slidePart in pptPart.SlideParts)
                        {
                            sb.AppendLine(slidePart.Slide.InnerText);
                        }
                    }

                    var txt = sb.ToString();
                    if (!string.IsNullOrWhiteSpace(txt))
                        return txt;
                }
                catch
                {
                    // ignore
                }

                // Это ZIP, но не docx/pptx → не считаем текстом
                return string.Empty;
            }

            // 3) Любые text/* → читаем как UTF-8
            if (contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
            {
                var txt = Encoding.UTF8.GetString(fileBytes);
                return txt;
            }

            // 4) Фолбэк: если байты "похожи" на текст — пробуем UTF-8, иначе пустая строка
            if (LooksLikeText(fileBytes))
            {
                return Encoding.UTF8.GetString(fileBytes);
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool LooksLikePdf(byte[] bytes) =>
        bytes.Length >= 4 &&
        bytes[0] == (byte)'%' &&
        bytes[1] == (byte)'P' &&
        bytes[2] == (byte)'D' &&
        bytes[3] == (byte)'F';

    private static bool LooksLikeZip(byte[] bytes) =>
        bytes.Length >= 2 &&
        bytes[0] == (byte)'P' &&
        bytes[1] == (byte)'K';

    // грубая эвристика: если слишком много "мусорных" байт — это не текст
    private static bool LooksLikeText(byte[] bytes, int sampleSize = 4096)
    {
        if (bytes.Length == 0) return false;
        var len = Math.Min(bytes.Length, sampleSize);
        int good = 0;

        for (int i = 0; i < len; i++)
        {
            var b = bytes[i];

            // разрешаем таб/перевод строки и т.д.
            if (b == 9 || b == 10 || b == 13)
            {
                good++;
                continue;
            }

            // печатные ASCII и простые расширенные
            if (b >= 32 && b <= 126)
            {
                good++;
                continue;
            }

            // часть UTF-8 high-bytes тоже учитываем как ок
            if (b >= 160)
            {
                good++;
                continue;
            }
        }

        var ratio = (double)good / len;
        return ratio > 0.85; // 85% байтов "нормальные"
    }

    private static string ComputeSha256(byte[] data)
    {
        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(data);
        return Convert.ToHexString(hashBytes);
    }

    private static HashSet<string> BuildShingles(string text, int k)
    {
        var normalized = text.ToLowerInvariant();

        var tokens = Regex
            .Split(normalized, @"\W+")
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        var result = new HashSet<string>();

        if (tokens.Count < k)
        {
            if (tokens.Count > 0)
            {
                result.Add(string.Join(" ", tokens));
            }
            return result;
        }

        for (int i = 0; i <= tokens.Count - k; i++)
        {
            var shingle = string.Join(" ", tokens.Skip(i).Take(k));
            result.Add(shingle);
        }

        return result;
    }

    private static double ComputeJaccardSimilarity(HashSet<string> a, string[] b)
    {
        if (a.Count == 0 && b.Length == 0)
            return 0.0;

        var setB = b.Length == 0 ? new HashSet<string>() : b.ToHashSet();

        var intersectionCount = a.Intersect(setB).Count();
        var unionCount = a.Count + setB.Count - intersectionCount;

        if (unionCount == 0)
            return 0.0;

        return (double)intersectionCount / unionCount;
    }

    /// <summary>
    /// Очищает текст для отправки в wordcloud:
    /// - оставляет только буквы/цифры/пробелы и немного пунктуации
    /// - нормализует пробелы
    /// - режет по длине
    /// </summary>
    private static string SanitizeForWordCloud(string text, int maxLength = 5000)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var sb = new StringBuilder(text.Length);

        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                sb.Append(' ');
            }
            else if (char.IsLetterOrDigit(ch) || ",.;:!?-_".Contains(ch))
            {
                sb.Append(ch);
            }
        }

        var result = sb.ToString();
        result = Regex.Replace(result, @"\s+", " ").Trim();

        if (result.Length > maxLength)
            result = result.Substring(0, maxLength);

        return result;
    }
}
