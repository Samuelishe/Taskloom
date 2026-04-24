using System.IO;

namespace Taskloom.Services.Records;

/// <summary>
/// Черновик прикреплённого аудиофайла записи.
/// </summary>
public sealed class RecordAudioDraft
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

    public string? DisplayTitle { get; set; }

    public double? DurationSeconds { get; set; }

    public string? AlbumTitle { get; set; }

    public string? Genre { get; set; }

    public string? CoverRelativePath { get; set; }

    public string? CoverPreviewPath { get; set; }

    public byte[]? CoverBytes { get; set; }

    public object? CoverSource => CoverBytes as object ?? CoverPreviewPath;

    public bool HasCover =>
        (CoverBytes?.Length ?? 0) > 0 ||
        !string.IsNullOrWhiteSpace(CoverPreviewPath) ||
        !string.IsNullOrWhiteSpace(CoverRelativePath);

    public bool IsPersisted => !string.IsNullOrWhiteSpace(RelativePath) && string.IsNullOrWhiteSpace(SourceFilePath);

    public bool IsPendingImport => !string.IsNullOrWhiteSpace(SourceFilePath);

    public string DisplayName => string.IsNullOrWhiteSpace(DisplayTitle)
        ? (!string.IsNullOrWhiteSpace(OriginalFileName)
            ? OriginalFileName
            : Path.GetFileName(SourceFilePath) ?? string.Empty)
        : DisplayTitle!;
}
