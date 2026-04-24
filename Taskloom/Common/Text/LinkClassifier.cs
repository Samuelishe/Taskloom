namespace Taskloom.Common.Text;

/// <summary>
/// Классифицирует распознанные ссылки по известным провайдерам и типам.
/// </summary>
public static class LinkClassifier
{
    public static IReadOnlyList<DetectedLink> DetectLinks(string? text)
    {
        return LinkParser.Parse(text)
            .Where(static token => token.IsLink)
            .Select(static token => Classify(token.Text, token.Target!))
            .ToArray();
    }

    public static DetectedLink Classify(string displayText, string target)
    {
        if (!Uri.TryCreate(target, UriKind.Absolute, out var uri))
        {
            return CreateGeneric(displayText, target);
        }

        var host = uri.Host.Trim().ToLowerInvariant();

        if (TryClassifyYouTube(displayText, uri, host, out var youTubeLink))
        {
            return youTubeLink;
        }

        if (TryClassifyTwitch(displayText, uri, host, out var twitchLink))
        {
            return twitchLink;
        }

        if (TryClassifyVkVideo(displayText, uri, host, out var vkLink))
        {
            return vkLink;
        }

        if (TryClassifyRuTube(displayText, uri, host, out var ruTubeLink))
        {
            return ruTubeLink;
        }

        return CreateGeneric(displayText, target);
    }

    private static bool TryClassifyYouTube(string displayText, Uri uri, string host, out DetectedLink link)
    {
        link = null!;

        if (host == "youtu.be")
        {
            var shortVideoId = uri.AbsolutePath.Trim('/');

            if (IsYouTubeVideoId(shortVideoId))
            {
                link = new DetectedLink(
                    displayText,
                    uri.AbsoluteUri,
                    $"https://www.youtube.com/watch?v={shortVideoId}",
                    DetectedLinkKind.YouTubeVideo,
                    "YouTube");

                return true;
            }
        }

        if (!host.EndsWith("youtube.com", StringComparison.Ordinal) &&
            !host.EndsWith("youtube-nocookie.com", StringComparison.Ordinal))
        {
            return false;
        }

        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        string? videoId = null;

        if (segments.Length == 0)
        {
            videoId = TryGetQueryParameter(uri, "v");
        }
        else if (string.Equals(segments[0], "watch", StringComparison.OrdinalIgnoreCase))
        {
            videoId = TryGetQueryParameter(uri, "v");
        }
        else if (segments.Length >= 2 &&
                 (string.Equals(segments[0], "embed", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(segments[0], "shorts", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(segments[0], "live", StringComparison.OrdinalIgnoreCase)))
        {
            videoId = segments[1];
        }

        if (!IsYouTubeVideoId(videoId))
        {
            return false;
        }

        link = new DetectedLink(
            displayText,
            uri.AbsoluteUri,
            $"https://www.youtube.com/watch?v={videoId}",
            DetectedLinkKind.YouTubeVideo,
            "YouTube");

        return true;
    }

    private static bool TryClassifyTwitch(string displayText, Uri uri, string host, out DetectedLink link)
    {
        link = null!;

        if (host == "clips.twitch.tv")
        {
            var clipSlug = uri.AbsolutePath.Trim('/');

            if (!string.IsNullOrWhiteSpace(clipSlug))
            {
                link = new DetectedLink(
                    displayText,
                    uri.AbsoluteUri,
                    $"https://clips.twitch.tv/{clipSlug}",
                    DetectedLinkKind.TwitchClip,
                    "Twitch");

                return true;
            }
        }

        if (!host.EndsWith("twitch.tv", StringComparison.Ordinal))
        {
            return false;
        }

        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length >= 2 &&
            string.Equals(segments[0], "videos", StringComparison.OrdinalIgnoreCase))
        {
            link = new DetectedLink(
                displayText,
                uri.AbsoluteUri,
                $"https://www.twitch.tv/videos/{segments[1]}",
                DetectedLinkKind.TwitchVideo,
                "Twitch");

            return true;
        }

        if (segments.Length >= 3 &&
            string.Equals(segments[1], "clip", StringComparison.OrdinalIgnoreCase))
        {
            link = new DetectedLink(
                displayText,
                uri.AbsoluteUri,
                $"https://www.twitch.tv/{segments[0]}/clip/{segments[2]}",
                DetectedLinkKind.TwitchClip,
                "Twitch");

            return true;
        }

        return false;
    }

    private static bool TryClassifyVkVideo(string displayText, Uri uri, string host, out DetectedLink link)
    {
        link = null!;

        if (!(host.EndsWith("vk.com", StringComparison.Ordinal) || host.EndsWith("vkvideo.ru", StringComparison.Ordinal)))
        {
            return false;
        }

        if (!uri.AbsolutePath.Contains("video", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        link = new DetectedLink(displayText, uri.AbsoluteUri, uri.AbsoluteUri, DetectedLinkKind.VkVideo, "VK Видео");
        return true;
    }

    private static bool TryClassifyRuTube(string displayText, Uri uri, string host, out DetectedLink link)
    {
        link = null!;

        if (!host.EndsWith("rutube.ru", StringComparison.Ordinal))
        {
            return false;
        }

        if (!uri.AbsolutePath.Contains("/video/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        link = new DetectedLink(displayText, uri.AbsoluteUri, uri.AbsoluteUri, DetectedLinkKind.RuTubeVideo, "RuTube");
        return true;
    }

    private static bool IsYouTubeVideoId(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Length is >= 6 and <= 20 &&
               value.All(static symbol => char.IsLetterOrDigit(symbol) || symbol is '-' or '_');
    }

    private static string? TryGetQueryParameter(Uri uri, string key)
    {
        var query = uri.Query.TrimStart('?');

        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = pair.IndexOf('=');
            var rawKey = separatorIndex >= 0 ? pair[..separatorIndex] : pair;

            if (!string.Equals(rawKey, key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var rawValue = separatorIndex >= 0 ? pair[(separatorIndex + 1)..] : string.Empty;
            return Uri.UnescapeDataString(rawValue);
        }

        return null;
    }

    private static DetectedLink CreateGeneric(string displayText, string target)
    {
        return new DetectedLink(displayText, target, target, DetectedLinkKind.Generic, "Link");
    }
}
