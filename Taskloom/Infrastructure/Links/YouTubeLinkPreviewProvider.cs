using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Net;
using Taskloom.Common.Text;

namespace Taskloom.Infrastructure.Links;

/// <summary>
/// Загружает заголовок и thumbnail для YouTube-видео.
/// </summary>
internal sealed class YouTubeLinkPreviewProvider(HttpClient httpClient) : ILinkPreviewProvider
{
    private readonly string? _apiKey = LinkPreviewApiOptionsProvider.LoadFromEnvironment().YouTubeApiKey;

    public DetectedLinkKind Kind => DetectedLinkKind.YouTubeVideo;

    public async Task<LinkPreviewFetchResult?> FetchAsync(DetectedLink link, CancellationToken cancellationToken)
    {
        if (!TryExtractVideoId(link.CanonicalUrl, out var videoId))
        {
            return null;
        }

        var oEmbedPayload = await TryGetOEmbedAsync(link.CanonicalUrl, cancellationToken);
        var detailsPayload = await TryGetVideoDetailsAsync(videoId, cancellationToken);
        var htmlFallback = await TryGetHtmlFallbackAsync(link.CanonicalUrl, cancellationToken);

        var title = !string.IsNullOrWhiteSpace(detailsPayload?.Items.FirstOrDefault()?.Snippet?.Title)
            ? detailsPayload!.Items[0].Snippet!.Title!.Trim()
            : !string.IsNullOrWhiteSpace(oEmbedPayload?.Title)
                ? oEmbedPayload!.Title!.Trim()
                : !string.IsNullOrWhiteSpace(htmlFallback?.Title)
                    ? htmlFallback!.Title!.Trim()
                : $"YouTube video";

        var description = !string.IsNullOrWhiteSpace(detailsPayload?.Items.FirstOrDefault()?.Snippet?.Description)
            ? detailsPayload!.Items[0].Snippet!.Description
            : htmlFallback?.Description;

        var durationText = FormatDuration(detailsPayload?.Items.FirstOrDefault()?.ContentDetails?.Duration);

        return new LinkPreviewFetchResult(
            title,
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            durationText,
            $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg",
            ".jpg");
    }

    private async Task<YouTubeOEmbedResponse?> TryGetOEmbedAsync(string canonicalUrl, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://www.youtube.com/oembed?format=json&url={Uri.EscapeDataString(canonicalUrl)}");

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<YouTubeOEmbedResponse>(stream, cancellationToken: cancellationToken);
    }

    private async Task<YouTubeVideosListResponse?> TryGetVideoDetailsAsync(string videoId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return null;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://www.googleapis.com/youtube/v3/videos?part=snippet,contentDetails&id={Uri.EscapeDataString(videoId)}&key={Uri.EscapeDataString(_apiKey)}");

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<YouTubeVideosListResponse>(stream, cancellationToken: cancellationToken);
    }

    private async Task<YouTubeHtmlFallbackData?> TryGetHtmlFallbackAsync(string canonicalUrl, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, canonicalUrl);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var title = ExtractMetaContent(html, "property", "og:title")
                    ?? ExtractJsonLdValue(html, "name")
                    ?? ExtractInitialPlayerResponseValue(html, "title")
                    ?? ExtractMetaContent(html, "name", "title")
                    ?? ExtractTitleTag(html);

        var description = ExtractMetaContent(html, "property", "og:description")
                          ?? ExtractJsonLdValue(html, "description")
                          ?? ExtractInitialPlayerResponseValue(html, "shortDescription")
                          ?? ExtractMetaContent(html, "name", "description");

        title = NormalizeHtmlText(title, suffixToTrim: " - YouTube");
        description = NormalizeHtmlText(description);

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return new YouTubeHtmlFallbackData(title, description);
    }

    private static bool TryExtractVideoId(string canonicalUrl, out string videoId)
    {
        videoId = string.Empty;

        if (!Uri.TryCreate(canonicalUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var value = TryGetQueryParameter(uri, "v");

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        videoId = value;
        return true;
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

    private sealed record YouTubeOEmbedResponse(string? Title);

    private sealed record YouTubeVideosListResponse(IReadOnlyList<YouTubeVideoItem> Items);

    private sealed record YouTubeVideoItem(YouTubeSnippet? Snippet, YouTubeContentDetails? ContentDetails);

    private sealed record YouTubeSnippet(string? Title, string? Description);

    private sealed record YouTubeContentDetails(string? Duration);

    private sealed record YouTubeHtmlFallbackData(string? Title, string? Description);

    private static string? ExtractMetaContent(string html, string attributeName, string attributeValue)
    {
        var pattern =
            $"""<meta[^>]*\b{Regex.Escape(attributeName)}\s*=\s*["']{Regex.Escape(attributeValue)}["'][^>]*\bcontent\s*=\s*["'](?<content>.*?)["'][^>]*>""";

        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (!match.Success)
        {
            pattern =
                $"""<meta[^>]*\bcontent\s*=\s*["'](?<content>.*?)["'][^>]*\b{Regex.Escape(attributeName)}\s*=\s*["']{Regex.Escape(attributeValue)}["'][^>]*>""";

            match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        return match.Success ? match.Groups["content"].Value : null;
    }

    private static string? ExtractTitleTag(string html)
    {
        var match = Regex.Match(html, @"<title>\s*(?<value>.*?)\s*</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups["value"].Value : null;
    }

    private static string? ExtractJsonLdValue(string html, string propertyName)
    {
        foreach (Match match in Regex.Matches(
                     html,
                     @"<script[^>]*type\s*=\s*[""']application/ld\+json[""'][^>]*>(?<json>.*?)</script>",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var rawJson = match.Groups["json"].Value;

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                continue;
            }

            try
            {
                var node = JsonNode.Parse(rawJson);
                var value = FindJsonValue(node, propertyName);

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
            catch
            {
                // Ignore malformed JSON-LD blocks and continue to the next fallback source.
            }
        }

        return null;
    }

    private static string? ExtractInitialPlayerResponseValue(string html, string propertyName)
    {
        var json = ExtractAssignedJsonObject(html, "ytInitialPlayerResponse");

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var node = JsonNode.Parse(json);
            return FindJsonValue(node, propertyName);
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractAssignedJsonObject(string html, string variableName)
    {
        var variableIndex = html.IndexOf(variableName, StringComparison.Ordinal);

        if (variableIndex < 0)
        {
            return null;
        }

        var assignmentIndex = html.IndexOf('=', variableIndex);

        if (assignmentIndex < 0)
        {
            return null;
        }

        var startIndex = html.IndexOf('{', assignmentIndex);

        if (startIndex < 0)
        {
            return null;
        }

        var depth = 0;
        var isInsideString = false;
        var isEscaped = false;

        for (var index = startIndex; index < html.Length; index++)
        {
            var current = html[index];

            if (isInsideString)
            {
                if (isEscaped)
                {
                    isEscaped = false;
                    continue;
                }

                if (current == '\\')
                {
                    isEscaped = true;
                    continue;
                }

                if (current == '"')
                {
                    isInsideString = false;
                }

                continue;
            }

            if (current == '"')
            {
                isInsideString = true;
                continue;
            }

            if (current == '{')
            {
                depth++;
                continue;
            }

            if (current != '}')
            {
                continue;
            }

            depth--;

            if (depth == 0)
            {
                return html[startIndex..(index + 1)];
            }
        }

        return null;
    }

    private static string? FindJsonValue(JsonNode? node, string propertyName)
    {
        if (node is null)
        {
            return null;
        }

        if (node is JsonValue valueNode &&
            valueNode.TryGetValue<string>(out var stringValue) &&
            !string.IsNullOrWhiteSpace(stringValue))
        {
            return stringValue;
        }

        if (node is JsonObject objectNode)
        {
            foreach (var pair in objectNode)
            {
                if (string.Equals(pair.Key, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    var directValue = FindJsonValue(pair.Value, propertyName);

                    if (!string.IsNullOrWhiteSpace(directValue))
                    {
                        return directValue;
                    }
                }

                var nestedValue = FindJsonValue(pair.Value, propertyName);

                if (!string.IsNullOrWhiteSpace(nestedValue))
                {
                    return nestedValue;
                }
            }
        }

        if (node is JsonArray arrayNode)
        {
            foreach (var child in arrayNode)
            {
                var nestedValue = FindJsonValue(child, propertyName);

                if (!string.IsNullOrWhiteSpace(nestedValue))
                {
                    return nestedValue;
                }
            }
        }

        return null;
    }

    private static string? NormalizeHtmlText(string? value, string? suffixToTrim = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var decoded = WebUtility.HtmlDecode(value).Trim();
        decoded = Regex.Replace(decoded, @"\s+", " ");

        if (!string.IsNullOrWhiteSpace(suffixToTrim) &&
            decoded.EndsWith(suffixToTrim, StringComparison.OrdinalIgnoreCase))
        {
            decoded = decoded[..^suffixToTrim.Length].TrimEnd();
        }

        return string.IsNullOrWhiteSpace(decoded) ? null : decoded;
    }

    private static string? FormatDuration(string? iso8601Duration)
    {
        if (string.IsNullOrWhiteSpace(iso8601Duration))
        {
            return null;
        }

        try
        {
            var duration = System.Xml.XmlConvert.ToTimeSpan(iso8601Duration);

            if (duration.TotalHours >= 1)
            {
                return duration.ToString(@"h\:mm\:ss");
            }

            return duration.ToString(@"m\:ss");
        }
        catch
        {
            return null;
        }
    }
}
