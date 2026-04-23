using System.Text.RegularExpressions;

namespace Taskloom.Common.Text;

/// <summary>
/// Разбивает произвольный текст на обычные фрагменты и кликабельные ссылки.
/// </summary>
public static partial class LinkParser
{
    private static readonly char[] TrailingPunctuation = ['.', ',', ';', ':', '!', '?', ')', ']', '}'];

    public static IReadOnlyList<LinkToken> Parse(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var tokens = new List<LinkToken>();
        var currentIndex = 0;

        foreach (Match match in LinkRegex().Matches(text))
        {
            if (!match.Success)
            {
                continue;
            }

            var candidate = match.Value;
            var trimmedCandidate = candidate.TrimEnd(TrailingPunctuation);

            if (string.IsNullOrWhiteSpace(trimmedCandidate))
            {
                continue;
            }

            var trimmedLength = trimmedCandidate.Length;
            var matchIndex = match.Index;

            if (matchIndex > currentIndex)
            {
                tokens.Add(new LinkToken(text[currentIndex..matchIndex]));
            }

            var target = NormalizeTarget(trimmedCandidate);

            if (target is null)
            {
                tokens.Add(new LinkToken(trimmedCandidate));
            }
            else
            {
                tokens.Add(new LinkToken(trimmedCandidate, target));
            }

            if (trimmedLength < candidate.Length)
            {
                tokens.Add(new LinkToken(candidate[trimmedLength..]));
            }

            currentIndex = matchIndex + candidate.Length;
        }

        if (currentIndex < text.Length)
        {
            tokens.Add(new LinkToken(text[currentIndex..]));
        }

        return tokens;
    }

    private static string? NormalizeTarget(string candidate)
    {
        if (EmailRegex().IsMatch(candidate))
        {
            var emailTarget = $"mailto:{candidate}";
            return IsValidAbsoluteUri(emailTarget) ? emailTarget : null;
        }

        if (candidate.Contains("://", StringComparison.Ordinal) ||
            candidate.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return IsValidAbsoluteUri(candidate) ? candidate : null;
        }

        if (Ipv4Regex().IsMatch(candidate))
        {
            var ipTarget = $"http://{candidate}";
            return IsValidAbsoluteUri(ipTarget) ? ipTarget : null;
        }

        if (DomainRegex().IsMatch(candidate))
        {
            var domainTarget = $"https://{candidate}";
            return IsValidAbsoluteUri(domainTarget) ? domainTarget : null;
        }

        return null;
    }

    private static bool IsValidAbsoluteUri(string target)
    {
        return Uri.TryCreate(target, UriKind.Absolute, out _);
    }

    [GeneratedRegex(
        @"(?ix)
        (
            \b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}\b
            |
            \b[A-Z][A-Z0-9+\-.]*:(?://)?[^\s<>'""]+
            |
            \b(?:\d{1,3}\.){3}\d{1,3}(?::\d{1,5})?(?:/[^\s<>'""]*)?
            |
            \b(?:www\.)?(?:[A-Z0-9\-]+\.)+[A-Z]{2,}(?::\d{1,5})?(?:/[^\s<>'""]*)?
        )")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"(?ix)\b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}\b")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^(?:(?:25[0-5]|2[0-4]\d|1?\d?\d)\.){3}(?:25[0-5]|2[0-4]\d|1?\d?\d)(?::\d{1,5})?(?:/.*)?$")]
    private static partial Regex Ipv4Regex();

    [GeneratedRegex(@"^(?:www\.)?(?:[A-Z0-9\-]+\.)+[A-Z]{2,}(?::\d{1,5})?(?:/.*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex DomainRegex();
}
