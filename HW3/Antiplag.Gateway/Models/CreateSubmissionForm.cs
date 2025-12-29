namespace Antiplag.Gateway.Models;

/// <summary>
/// Форма создания отправки работы (данные + файл).
/// </summary>
public class CreateSubmissionForm
{
    // Загружаемый файл (multipart/form-data)
    public IFormFile File { get; set; } = null!;

    // Идентификатор работы (внешний/внутренний)
    public string WorkId { get; set; } = null!;

    // Идентификатор студента
    public string StudentId { get; set; } = null!;

    // Имя студента
    public string StudentName { get; set; } = null!;

    // Группа (необязательно)
    public string? Group { get; set; }
}