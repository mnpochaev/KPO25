namespace Antiplag.FileStorage.Models;

/// <summary>
/// Метаданные сохранённого файла (модель для хранения в БД).
/// </summary>
public class StoredFile
{
    // Идентификатор файла
    public Guid Id { get; set; }

    // Оригинальное имя при загрузке
    public string OriginalName { get; set; } = null!;

    // MIME-тип (Content-Type)
    public string ContentType { get; set; } = null!;

    // Размер файла в байтах
    public long SizeBytes { get; set; }

    // Путь к файлу в файловой системе/хранилище
    public string Path { get; set; } = null!;

    // Время загрузки
    public DateTime UploadedAt { get; set; }
}