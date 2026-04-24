using Taskloom.Common.Text;

namespace Taskloom.Services.Links;

/// <summary>
/// Результат загрузки или чтения preview-данных внешней видео-ссылки.
/// </summary>
public sealed record LinkPreviewData(
    string Url,
    string CanonicalUrl,
    string Title,
    string SourceDisplayName,
    DetectedLinkKind Kind,
    string? Description,
    string? DurationText,
    string? ThumbnailPath,
    bool HasThumbnail)
{
    public bool IsPlaceholder => !HasThumbnail || string.IsNullOrWhiteSpace(ThumbnailPath);
}
