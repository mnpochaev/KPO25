using System;
using System.Collections.Generic;
using System.Linq;

namespace Antiplag.Analysis.Models;

/// <summary>
/// Сводка по проверке одной работы (workId) + список всех отчётов.
/// </summary>
public class WorkReportSummaryDto
{
    public string WorkId { get; set; } = null!;

    public int TotalSubmissions { get; set; }
    public int PlagiarizedCount { get; set; }

    /// <summary>
    /// Доля плагиата от 0 до 1.
    /// </summary>
    public double PlagiarizedShare { get; set; }

    /// <summary>
    /// Средняя схожесть по всем сдачам (0..1).
    /// </summary>
    public double AverageSimilarity { get; set; }

    /// <summary>
    /// Максимальная схожесть по всем сдачам (0..1).
    /// </summary>
    public double MaxSimilarity { get; set; }

    /// <summary>
    /// Количество уникальных студентов.
    /// </summary>
    public int UniqueStudents { get; set; }

    public DateTime? FirstSubmissionAt { get; set; }
    public DateTime? LastSubmissionAt { get; set; }

    /// <summary>
    /// Детальные записи по каждой сдаче.
    /// </summary>
    public IEnumerable<WorkReportItemDto> Items { get; set; } = Enumerable.Empty<WorkReportItemDto>();
}