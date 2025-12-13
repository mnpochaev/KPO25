using System;

namespace Antiplag.Analysis.Models;

/// <summary>
/// Краткая информация по одной сдаче для конкретной работы.
/// Используется в аналитике для преподавателя.
/// </summary>
public class WorkReportItemDto
{
    public Guid SubmissionId { get; set; }
    public string StudentId { get; set; } = null!;
    public Guid FileId { get; set; }
    public bool Plagiarism { get; set; }
    public double Similarity { get; set; }
    public Guid? OriginalSubmissionId { get; set; }
    public string? OriginalStudentId { get; set; }
    public DateTime CreatedAt { get; set; }
}