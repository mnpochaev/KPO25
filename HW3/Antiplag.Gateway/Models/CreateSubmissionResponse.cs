namespace Antiplag.Gateway.Models;

/// <summary>
/// DTO ответа после создания отправки и выполнения проверки (результаты + метаданные).
/// </summary>
public class CreateSubmissionResponse
{
    // Идентификатор отправки (submission)
    public Guid SubmissionId { get; set; }

    // Идентификатор сохранённого файла
    public Guid FileId { get; set; }

    // Идентификатор работы
    public string WorkId { get; set; } = null!;

    // Идентификатор студента
    public string StudentId { get; set; } = null!;

    // Имя студента
    public string StudentName { get; set; } = null!;

    // Группа (необязательно)
    public string? Group { get; set; }

    // Флаг плагиата по логике сервиса
    public bool Plagiarism { get; set; }

    // Доля схожести (0..1 или 0..100 — зависит от вашей логики)
    public double Similarity { get; set; }

    // Ссылка на оригинальную (совпавшую) отправку, если найдена
    public Guid? OriginalSubmissionId { get; set; }

    // StudentId автора оригинала, если найден
    public string? OriginalStudentId { get; set; }
    
    // URL облака слов (если формируется/хранится отдельно)
    public string? WordCloudUrl { get; set; }
}