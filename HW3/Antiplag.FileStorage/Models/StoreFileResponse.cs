namespace Antiplag.FileStorage.Models;

/// <summary>
/// DTO ответа при успешной загрузке файла.
/// </summary>
public class StoreFileResponse
{
    // Идентификатор сохранённого файла
    public Guid FileId { get; set; }

    // Оригинальное имя файла
    public string OriginalName { get; set; } = null!;

    // MIME-тип (Content-Type)
    public string ContentType { get; set; } = null!;

    // Размер файла в байтах
    public long SizeBytes { get; set; }

    // Время загрузки
    public DateTime UploadedAt { get; set; }
}