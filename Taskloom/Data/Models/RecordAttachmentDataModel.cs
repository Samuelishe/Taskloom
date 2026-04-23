namespace Taskloom.Data.Models;

/// <summary>
/// Модель хранения вложения записи для SQLite.
/// </summary>
public sealed class RecordAttachmentDataModel
{
    public string Id { get; set; } = string.Empty;

    public string RecordId { get; set; } = string.Empty;

    public int KindId { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public long FileSize { get; set; }

    public string CreatedUtc { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public string? DisplayTitle { get; set; }

    public double? DurationSeconds { get; set; }

    public string? PreviewRelativePath { get; set; }
}
