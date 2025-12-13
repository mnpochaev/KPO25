namespace Antiplag.Gateway.Models;

/// <summary>
/// Сводка по отчётам для одной работы (агрегаты + список отправок).
/// </summary>
public class WorkReportsSummary
{
    // Идентификатор работы
    public string WorkId { get; set; } = null!;

    // Всего отправок
    public int TotalSubmissions { get; set; }

    // Кол-во отправок, помеченных как плагиат
    public int PlagiarizedCount { get; set; }

    // Доля плагиата (0..1 или 0..100 — зависит от вашей логики)
    public double PlagiarizedShare { get; set; }

    // Средняя схожесть по всем отправкам
    public double AverageSimilarity { get; set; }

    // Максимальная схожесть среди отправок
    public double MaxSimilarity { get; set; }

    // Кол-во уникальных студентов
    public int UniqueStudents { get; set; }

    // Время первой отправки (если есть)
    public DateTime? FirstSubmissionAt { get; set; }

    // Время последней отправки (если есть)
    public DateTime? LastSubmissionAt { get; set; }

    // Детализация по отправкам
    public List<WorkReportItem> Items { get; set; } = new();
}