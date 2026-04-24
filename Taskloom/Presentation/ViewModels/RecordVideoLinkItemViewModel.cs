using CommunityToolkit.Mvvm.ComponentModel;
using Taskloom.Common.Text;
using Taskloom.Services.Links;

namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// Presentation-модель preview-карточки внешней видео-ссылки.
/// </summary>
public sealed partial class RecordVideoLinkItemViewModel : ObservableObject
{
    private readonly Guid _recordId;
    private readonly DetectedLink _link;
    private readonly ILinkPreviewService _linkPreviewService;
    private readonly string _placeholderTitle;

    public RecordVideoLinkItemViewModel(
        Guid recordId,
        DetectedLink link,
        ILinkPreviewService linkPreviewService,
        string loadingTitle,
        string placeholderTitle)
    {
        _recordId = recordId;
        _link = link ?? throw new ArgumentNullException(nameof(link));
        _linkPreviewService = linkPreviewService ?? throw new ArgumentNullException(nameof(linkPreviewService));
        _placeholderTitle = placeholderTitle;
        Title = string.IsNullOrWhiteSpace(loadingTitle) ? link.DisplayText : loadingTitle;
        SourceDisplayName = link.SourceDisplayName;
        Url = link.OriginalUrl;
        ToolTipText = link.OriginalUrl;

        _ = LoadAsync();
    }

    [ObservableProperty]
    private string title;

    [ObservableProperty]
    private string? thumbnailPath;

    [ObservableProperty]
    private bool hasThumbnail;

    [ObservableProperty]
    private string? description;

    [ObservableProperty]
    private string? durationText;

    [ObservableProperty]
    private string toolTipText;

    public string SourceDisplayName { get; }

    public string Url { get; }

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    public bool HasDuration => !string.IsNullOrWhiteSpace(DurationText);

    public string SecondaryMetaLine =>
        HasDuration
            ? $"{SourceDisplayName} • {DurationText}"
            : SourceDisplayName;

    public string DescriptionToolTip =>
        string.IsNullOrWhiteSpace(Description)
            ? ToolTipText
            : Description;

    partial void OnDescriptionChanged(string? value)
    {
        OnPropertyChanged(nameof(HasDescription));
        OnPropertyChanged(nameof(DescriptionToolTip));
    }

    partial void OnDurationTextChanged(string? value)
    {
        OnPropertyChanged(nameof(HasDuration));
        OnPropertyChanged(nameof(SecondaryMetaLine));
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var preview = await _linkPreviewService.GetOrRefreshPreviewAsync(_recordId, _link, cancellationToken);
            Title = preview.Title;
            ThumbnailPath = preview.ThumbnailPath;
            HasThumbnail = preview.HasThumbnail;
            Description = preview.Description;
            DurationText = preview.DurationText;
            ToolTipText = preview.CanonicalUrl;

            if (!preview.HasThumbnail && string.IsNullOrWhiteSpace(Title))
            {
                Title = _placeholderTitle;
            }
        }
        catch
        {
            Title = _placeholderTitle;
            ThumbnailPath = null;
            HasThumbnail = false;
            Description = null;
            DurationText = null;
            ToolTipText = _link.OriginalUrl;
        }
    }
}
