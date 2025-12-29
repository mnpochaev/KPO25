using System;

namespace Antiplag.Analysis.Models;

/// <summary>
/// Результат проверки работы на плагиат.
/// </summary>
public class ReportDto
{
    public Guid ReportId { get; set; }
    public Guid SubmissionId { get; set; }

    /// <summary>
    /// True, если найдена похожая работа другого студента выше порога.
    /// </summary>
    public bool Plagiarism { get; set; }

    /// <summary>
    /// Оценка схожести от 0 до 1 (0%..100%).
    /// </summary>
    public double Similarity { get; set; }

    /// <summary>
    /// ID оригинальной работы, с которой обнаружено сильное совпадение.
    /// </summary>
    public Guid? OriginalSubmissionId { get; set; }
    
    /// <summary>
    /// ID автора оригинальной работы, с которой обнаружено сильное совпадение.
    /// </summary>
    public string? OriginalStudentId { get; set; }

    /// <summary>
    /// URL облака слов (пока можно не заполнять).
    /// </summary>
    public string? WordCloudUrl { get; set; }

    public DateTime CreatedAt { get; set; }
}