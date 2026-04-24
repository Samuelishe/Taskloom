namespace Taskloom.Common.Text;

/// <summary>
/// Описывает распознанную и нормализованную ссылку из текста записи.
/// </summary>
public sealed record DetectedLink(
    string DisplayText,
    string OriginalUrl,
    string CanonicalUrl,
    DetectedLinkKind Kind,
    string SourceDisplayName)
{
    public bool IsVideoLink => Kind != DetectedLinkKind.Generic;
}
