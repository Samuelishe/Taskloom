namespace Taskloom.Common.Text;

/// <summary>
/// Готовит display-only текст описания записи без изменения исходных данных.
/// </summary>
public static class RecordDetailsDisplayFormatter
{
    public static string? Build(string? details, bool hideLinksWhenPreviewAvailable, IEnumerable<DetectedLink> previewLinks)
    {
        if (string.IsNullOrWhiteSpace(details))
        {
            return null;
        }

        if (!hideLinksWhenPreviewAvailable)
        {
            return details;
        }

        var hiddenCanonicalUrls = previewLinks
            .Select(static link => link.CanonicalUrl)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (hiddenCanonicalUrls.Count == 0)
        {
            return details;
        }

        var lines = details.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');

        var visibleLines = new List<string>();
        var previousWasBlank = false;

        foreach (var line in lines)
        {
            if (ShouldHideStandaloneVideoLinkLine(line, hiddenCanonicalUrls))
            {
                continue;
            }

            var isBlank = string.IsNullOrWhiteSpace(line);

            if (isBlank && (visibleLines.Count == 0 || previousWasBlank))
            {
                continue;
            }

            visibleLines.Add(line);
            previousWasBlank = isBlank;
        }

        while (visibleLines.Count > 0 && string.IsNullOrWhiteSpace(visibleLines[^1]))
        {
            visibleLines.RemoveAt(visibleLines.Count - 1);
        }

        return visibleLines.Count == 0
            ? null
            : string.Join(Environment.NewLine, visibleLines);
    }

    private static bool ShouldHideStandaloneVideoLinkLine(string line, IReadOnlySet<string> hiddenCanonicalUrls)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var tokens = LinkParser.Parse(line);
        var linkTokens = tokens.Where(static token => token.IsLink).ToArray();

        if (linkTokens.Length != 1)
        {
            return false;
        }

        if (tokens.Any(static token => !token.IsLink && !string.IsNullOrWhiteSpace(token.Text)))
        {
            return false;
        }

        var linkToken = linkTokens[0];
        var detectedLink = LinkClassifier.Classify(linkToken.Text, linkToken.Target!);

        return detectedLink.IsVideoLink &&
               hiddenCanonicalUrls.Contains(detectedLink.CanonicalUrl);
    }
}
