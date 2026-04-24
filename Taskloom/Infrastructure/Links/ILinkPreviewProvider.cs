using Taskloom.Common.Text;

namespace Taskloom.Infrastructure.Links;

/// <summary>
/// Загружает preview-данные для конкретного провайдера внешних ссылок.
/// </summary>
internal interface ILinkPreviewProvider
{
    DetectedLinkKind Kind { get; }

    Task<LinkPreviewFetchResult?> FetchAsync(DetectedLink link, CancellationToken cancellationToken);
}
