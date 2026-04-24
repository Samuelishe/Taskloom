using Taskloom.Common.Text;

namespace Taskloom.Services.Links;

/// <summary>
/// Классифицирует внешние ссылки и получает preview-данные для поддерживаемых провайдеров.
/// </summary>
public interface ILinkPreviewService
{
    IReadOnlyList<DetectedLink> DetectLinks(string? text);

    bool CanPreview(DetectedLink link);

    Task<LinkPreviewData> GetOrRefreshPreviewAsync(Guid recordId, DetectedLink link, CancellationToken cancellationToken = default);
}
