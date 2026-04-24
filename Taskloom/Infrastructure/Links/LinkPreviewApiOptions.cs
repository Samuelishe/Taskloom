namespace Taskloom.Infrastructure.Links;

/// <summary>
/// Бесплатные API-учётные данные для enrichment внешних видео-ссылок.
/// </summary>
public sealed class LinkPreviewApiOptions
{
    public string? YouTubeApiKey { get; init; }

    public string? TwitchClientId { get; init; }

    public string? TwitchClientSecret { get; init; }
}
