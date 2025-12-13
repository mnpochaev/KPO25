namespace Antiplag.Gateway.Models;

/// <summary>
/// Элемент отчёта по работе (результат проверки одной отправки).
/// </summary>
public class WorkReportItem
{
    // Идентификатор отправки
    public Guid SubmissionId { get; set; }

    // Идентификатор студента-автора отправки
    public string StudentId { get; set; } = null!;

    // Идентификатор файла в FileStorage
    public Guid FileId { get; set; }

    // Флаг плагиата по порогу/правилу
    public bool Plagiarism { get; set; }

    // Схожесть (формат зависит от вашей логики)
    public double Similarity { get; set; }

    // Идентификатор оригинальной отправки (если найдено совпадение)
    public Guid? OriginalSubmissionId { get; set; }

    // StudentId автора оригинала (если найдено совпадение)
    public string? OriginalStudentId { get; set; }

    // Время создания записи отчёта
    public DateTime CreatedAt { get; set; }
}