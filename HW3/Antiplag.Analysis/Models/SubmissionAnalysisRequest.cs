using System;

namespace Antiplag.Analysis.Models;

/// <summary>
/// Запрос на анализ конкретной сдачи.
/// Получает от Gateway ID файла и метаданные работы.
/// </summary>
public class SubmissionAnalysisRequest
{
    public Guid SubmissionId { get; set; }
    public Guid FileId { get; set; }
    public string WorkId { get; set; } = null!;
    public string StudentId { get; set; } = null!;
}
