using System.IO;

namespace Taskloom.Services.Records;

/// <summary>
/// Черновик прикреплённого изображения записи.
/// </summary>
public sealed class RecordImageDraft
{
    public Guid? Id { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string? RelativePath { get; set; }

    public string? ContentType { get; set; }

    public long FileSize { get; set; }

    public DateTime CreatedUtc { get; set; }

    public int SortOrder { get; set; }

    public string? SourceFilePath { get; set; }

    public string? PreviewPath { get; set; }

    public bool IsPersisted => !string.IsNullOrWhiteSpace(RelativePath) && string.IsNullOrWhiteSpace(SourceFilePath);

    public bool IsPendingImport => !string.IsNullOrWhiteSpace(SourceFilePath);

    public string DisplayName => string.IsNullOrWhiteSpace(OriginalFileName)
        ? Path.GetFileName(SourceFilePath) ?? string.Empty
        : OriginalFileName;
}
