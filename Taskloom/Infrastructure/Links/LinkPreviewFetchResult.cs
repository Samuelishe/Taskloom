namespace Taskloom.Infrastructure.Links;

/// <summary>
/// Результат сетевой загрузки данных preview у внешнего провайдера.
/// </summary>
internal sealed record LinkPreviewFetchResult(
    string Title,
    string? Description,
    string? DurationText,
    string ThumbnailUrl,
    string ThumbnailFileExtension);
