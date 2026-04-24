namespace Taskloom.Infrastructure.Links;

/// <summary>
/// Читает API-настройки enrichment из переменных окружения процесса.
/// </summary>
public static class LinkPreviewApiOptionsProvider
{
    public static LinkPreviewApiOptions LoadFromEnvironment()
    {
        return new LinkPreviewApiOptions
        {
            YouTubeApiKey = GetTrimmedEnvironmentVariable("TASKLOOM_YOUTUBE_API_KEY"),
            TwitchClientId = GetTrimmedEnvironmentVariable("TASKLOOM_TWITCH_CLIENT_ID"),
            TwitchClientSecret = GetTrimmedEnvironmentVariable("TASKLOOM_TWITCH_CLIENT_SECRET")
        };
    }

    private static string? GetTrimmedEnvironmentVariable(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
