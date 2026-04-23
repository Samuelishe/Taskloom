namespace Taskloom.Domain;

/// <summary>
/// Вложение, привязанное к календарной записи.
/// </summary>
public sealed class RecordAttachment
{
    public RecordAttachment(
        Guid id,
        Guid recordId,
        RecordAttachmentKind kind,
        string originalFileName,
        string storedFileName,
        string relativePath,
        string? contentType,
        long fileSize,
        DateTime createdUtc,
        int sortOrder = 0,
        string? displayTitle = null,
        double? durationSeconds = null,
        string? previewRelativePath = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        RecordId = recordId == Guid.Empty ? throw new ArgumentException("Идентификатор записи обязателен.", nameof(recordId)) : recordId;
        Kind = kind;
        OriginalFileName = NormalizeRequiredText(originalFileName, nameof(originalFileName), 260);
        StoredFileName = NormalizeRequiredText(storedFileName, nameof(storedFileName), 260);
        RelativePath = NormalizeRequiredText(relativePath, nameof(relativePath), 512);
        ContentType = NormalizeOptionalText(contentType, 100);

        if (fileSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileSize), "Размер файла не может быть отрицательным.");
        }

        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), "Порядок изображения не может быть отрицательным.");
        }

        if (durationSeconds is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Длительность медиа не может быть отрицательной.");
        }

        FileSize = fileSize;
        CreatedUtc = createdUtc.Kind == DateTimeKind.Utc
            ? createdUtc
            : DateTime.SpecifyKind(createdUtc, DateTimeKind.Utc);
        SortOrder = sortOrder;
        DisplayTitle = NormalizeOptionalText(displayTitle, 260);
        DurationSeconds = durationSeconds;
        PreviewRelativePath = NormalizeOptionalText(previewRelativePath, 512);
    }

    public Guid Id { get; }

    public Guid RecordId { get; }

    public RecordAttachmentKind Kind { get; }

    public string OriginalFileName { get; }

    public string StoredFileName { get; }

    public string RelativePath { get; }

    public string? ContentType { get; }

    public long FileSize { get; }

    public DateTime CreatedUtc { get; }

    public int SortOrder { get; }

    public bool IsImage => Kind == RecordAttachmentKind.Image;

    public bool IsAudio => Kind == RecordAttachmentKind.Audio;

    public string? DisplayTitle { get; }

    public double? DurationSeconds { get; }

    public string? PreviewRelativePath { get; }

    private static string NormalizeRequiredText(string value, string paramName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, $"Значение не должно превышать {maxLength} символов.");
        }

        return normalizedValue;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Значение не должно превышать {maxLength} символов.");
        }

        return normalizedValue;
    }
}
