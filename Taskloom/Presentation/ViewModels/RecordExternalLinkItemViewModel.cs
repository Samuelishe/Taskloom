using Taskloom.Common.Text;

namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// Presentation-модель обычной внешней ссылки без preview-карточки.
/// </summary>
public sealed class RecordExternalLinkItemViewModel
{
    public RecordExternalLinkItemViewModel(DetectedLink link)
    {
        ArgumentNullException.ThrowIfNull(link);
        Url = link.OriginalUrl;
        DisplayText = string.IsNullOrWhiteSpace(link.DisplayText) ? link.OriginalUrl : link.DisplayText;
        SourceDisplayName = link.SourceDisplayName;
    }

    public string Url { get; }

    public string DisplayText { get; }

    public string SourceDisplayName { get; }
}
