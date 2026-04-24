namespace Taskloom.Common.Text;

/// <summary>
/// Классификация распознанной ссылки для дальнейшего обогащения.
/// </summary>
public enum DetectedLinkKind
{
    Generic = 0,
    YouTubeVideo = 1,
    TwitchClip = 2,
    TwitchVideo = 3,
    VkVideo = 4,
    RuTubeVideo = 5
}
