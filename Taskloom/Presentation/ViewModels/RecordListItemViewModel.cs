using CommunityToolkit.Mvvm.ComponentModel;
using Taskloom.Domain;
using Taskloom.Common.Text;
using Taskloom.Services.Localization;
using Taskloom.Services.Links;
using Taskloom.Services.Media;
using Taskloom.Services.Records;

namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// Модель элемента списка записей выбранной даты.
/// </summary>
public sealed partial class RecordListItemViewModel : ObservableObject, IDisposable
{
    private RecordListItemViewModel(
        Guid id,
        RecordType type,
        string typeDisplayName,
        string title,
        string? details,
        string? displayDetails,
        int sortOrder,
        bool hideLinksWhenPreviewAvailable,
        string timeDisplay,
        bool isCompleted,
        DateTime createdUtc,
        IReadOnlyList<RecordImageListItemViewModel>? images = null,
        IReadOnlyList<RecordAudioListItemViewModel>? audios = null,
        IReadOnlyList<RecordVideoLinkItemViewModel>? videoLinks = null,
        IReadOnlyList<RecordExternalLinkItemViewModel>? overflowVideoLinks = null,
        string? locationDisplay = null,
        EventStatus? eventStatus = null,
        string? eventStatusText = null,
        string? taskStatusText = null,
        string? taskStatusIcon = null)
    {
        Id = id;
        Type = type;
        TypeDisplayName = typeDisplayName;
        Title = title;
        Details = details;
        DisplayDetails = displayDetails;
        SortOrder = sortOrder;
        HideLinksWhenPreviewAvailable = hideLinksWhenPreviewAvailable;
        TimeDisplay = timeDisplay;
        IsCompleted = isCompleted;
        CreatedUtc = createdUtc;
        Images = images ?? [];
        Audios = audios ?? [];
        VideoLinks = videoLinks ?? [];
        OverflowVideoLinks = overflowVideoLinks ?? [];
        VisibleImages = Images.Take(9).ToArray();
        LocationDisplay = locationDisplay;
        EventStatus = eventStatus;
        EventStatusText = eventStatusText;
        TaskStatusText = taskStatusText;
        TaskStatusIcon = taskStatusIcon;
    }

    public Guid Id { get; }

    public RecordType Type { get; }

    public string TypeDisplayName { get; }

    public string Title { get; }

    public string? Details { get; }

    public string? DisplayDetails { get; }

    public int SortOrder { get; }

    public bool HideLinksWhenPreviewAvailable { get; }

    public string TimeDisplay { get; }

    public bool IsCompleted { get; }

    public DateTime CreatedUtc { get; }

    public string CreatedAtDisplay => CreatedUtc.ToLocalTime().ToString("HH:mm");

    public bool IsTask => Type == RecordType.Task;

    public bool IsEvent => Type == RecordType.Event;

    public bool HasLocation => !string.IsNullOrWhiteSpace(LocationDisplay);

    public bool HasDisplayDetails => !string.IsNullOrWhiteSpace(DisplayDetails);

    public bool HasImages => Images.Count > 0;

    public bool HasAudios => Audios.Count > 0;

    public bool HasVideoLinks => VideoLinks.Count > 0;

    public bool HasOverflowVideoLinks => OverflowVideoLinks.Count > 0;

    public bool HasSingleImage => Images.Count == 1;

    public bool HasTwoImages => Images.Count == 2;

    public bool HasThreeImages => Images.Count == 3;

    public bool HasFourToSixImages => Images.Count is >= 4 and <= 6;

    public bool HasSevenOrMoreImages => Images.Count >= 7;

    public IReadOnlyList<RecordImageListItemViewModel> Images { get; }

    public IReadOnlyList<RecordAudioListItemViewModel> Audios { get; }

    public IReadOnlyList<RecordVideoLinkItemViewModel> VideoLinks { get; }

    public IReadOnlyList<RecordExternalLinkItemViewModel> OverflowVideoLinks { get; }

    public IReadOnlyList<RecordImageListItemViewModel> VisibleImages { get; }

    public int ImageCount => Images.Count;

    public RecordImageListItemViewModel? FirstImage => VisibleImages.ElementAtOrDefault(0);

    public RecordImageListItemViewModel? SecondImage => VisibleImages.ElementAtOrDefault(1);

    public RecordImageListItemViewModel? ThirdImage => VisibleImages.ElementAtOrDefault(2);

    public RecordImageListItemViewModel? FourthImage => VisibleImages.ElementAtOrDefault(3);

    public RecordImageListItemViewModel? FifthImage => VisibleImages.ElementAtOrDefault(4);

    public RecordImageListItemViewModel? SixthImage => VisibleImages.ElementAtOrDefault(5);

    public RecordImageListItemViewModel? SeventhImage => VisibleImages.ElementAtOrDefault(6);

    public RecordImageListItemViewModel? EighthImage => VisibleImages.ElementAtOrDefault(7);

    public RecordImageListItemViewModel? NinthImage => VisibleImages.ElementAtOrDefault(8);

    public int AdditionalImageCount => Math.Max(0, Images.Count - VisibleImages.Count);

    public bool HasAdditionalImages => AdditionalImageCount > 0;

    public string? LocationDisplay { get; }

    public EventStatus? EventStatus { get; }

    public string? EventStatusText { get; }

    public string? TaskStatusText { get; }

    public string? TaskStatusIcon { get; }

    [ObservableProperty]
    private bool isDropTarget;

    [ObservableProperty]
    private bool isDragSource;


    public void Dispose()
    {
        foreach (var audio in Audios)
        {
            audio.Dispose();
        }
    }

    /// <summary>
    /// Создаёт presentation-модель элемента списка из доменной записи.
    /// </summary>
    public static RecordListItemViewModel Create(
        CalendarRecord record,
        ILocalizationService localizationService,
        ILinkPreviewService linkPreviewService,
        IRecordImageStorageService imageStorageService,
        IRecordAudioStorageService audioStorageService,
        IAudioPlaybackService audioPlaybackService)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(localizationService);
        ArgumentNullException.ThrowIfNull(linkPreviewService);
        ArgumentNullException.ThrowIfNull(imageStorageService);
        ArgumentNullException.ThrowIfNull(audioStorageService);
        ArgumentNullException.ThrowIfNull(audioPlaybackService);

        var images = record.ImageAttachments
            .Select(attachment => new RecordImageListItemViewModel(
                imageStorageService.GetAbsolutePath(attachment.RelativePath) ?? string.Empty,
                attachment.OriginalFileName))
            .Where(static item => !string.IsNullOrWhiteSpace(item.Path))
            .ToArray();

        var audios = record.AudioAttachments
            .Select(attachment => new RecordAudioListItemViewModel(
                audioStorageService.GetAbsolutePath(attachment.RelativePath) ?? string.Empty,
                attachment.OriginalFileName,
                string.IsNullOrWhiteSpace(attachment.DisplayTitle)
                    ? attachment.OriginalFileName
                    : attachment.DisplayTitle,
                audioStorageService.GetAbsolutePath(attachment.PreviewRelativePath),
                attachment.DurationSeconds,
                attachment.AlbumTitle,
                attachment.Genre,
                audioPlaybackService))
            .Where(static item => !string.IsNullOrWhiteSpace(item.Path))
            .ToArray();

        var detectedVideoLinks = linkPreviewService.DetectLinks(record.Details)
            .Where(static link => link.IsVideoLink)
            .ToArray();
        var previewableVideoLinks = new List<DetectedLink>();

        var videoPreviewItems = new List<RecordVideoLinkItemViewModel>();
        var overflowVideoLinks = new List<RecordExternalLinkItemViewModel>();

        foreach (var link in detectedVideoLinks)
        {
            if (videoPreviewItems.Count < 5 && linkPreviewService.CanPreview(link))
            {
                previewableVideoLinks.Add(link);
                videoPreviewItems.Add(new RecordVideoLinkItemViewModel(
                    record.Id,
                    link,
                    linkPreviewService,
                    localizationService.GetString("RecordLinkPreview.LoadingTitle"),
                    localizationService.GetString("RecordLinkPreview.PlaceholderTitle")));

                continue;
            }

            overflowVideoLinks.Add(new RecordExternalLinkItemViewModel(link));
        }

        var displayDetails = RecordDetailsDisplayFormatter.Build(
            record.Details,
            record.HideLinksWhenPreviewAvailable,
            previewableVideoLinks);

        return record switch
        {
            TaskRecord taskRecord => new RecordListItemViewModel(
                taskRecord.Id,
                taskRecord.Type,
                localizationService.GetString("RecordType.Task"),
                taskRecord.Title,
                taskRecord.Details,
                displayDetails,
                taskRecord.SortOrder,
                taskRecord.HideLinksWhenPreviewAvailable,
                localizationService.GetString("RecordList.TaskLabel"),
                taskRecord.IsCompleted,
                taskRecord.CreatedUtc,
                images,
                audios,
                videoPreviewItems,
                overflowVideoLinks,
                null,
                null,
                null,
                taskRecord.IsCompleted
                    ? localizationService.GetString("RecordList.TaskCompleted")
                    : localizationService.GetString("RecordList.TaskPending"),
                taskRecord.IsCompleted ? "✓" : "✕"),

            NoteRecord noteRecord => new RecordListItemViewModel(
                noteRecord.Id,
                noteRecord.Type,
                localizationService.GetString("RecordType.Note"),
                noteRecord.Title,
                noteRecord.Details,
                displayDetails,
                noteRecord.SortOrder,
                noteRecord.HideLinksWhenPreviewAvailable,
                localizationService.GetString("RecordList.NoteAnyTime"),
                false,
                noteRecord.CreatedUtc,
                images,
                audios,
                videoPreviewItems,
                overflowVideoLinks),

            EventRecord eventRecord => new RecordListItemViewModel(
                eventRecord.Id,
                eventRecord.Type,
                localizationService.GetString("RecordType.Event"),
                eventRecord.Title,
                eventRecord.Details,
                displayDetails,
                eventRecord.SortOrder,
                eventRecord.HideLinksWhenPreviewAvailable,
                $"{eventRecord.StartTime:HH\\:mm} - {eventRecord.EndTime:HH\\:mm}",
                false,
                eventRecord.CreatedUtc,
                images,
                audios,
                videoPreviewItems,
                overflowVideoLinks,
                string.IsNullOrWhiteSpace(eventRecord.Location)
                    ? null
                    : localizationService.Format("RecordList.EventLocation", eventRecord.Location),
                eventRecord.Status,
                localizationService.GetString(GetEventStatusKey(eventRecord.Status))),

            DaySummaryRecord summaryRecord => new RecordListItemViewModel(
                summaryRecord.Id,
                summaryRecord.Type,
                localizationService.GetString("RecordType.DaySummary"),
                summaryRecord.Title,
                summaryRecord.Details,
                displayDetails,
                summaryRecord.SortOrder,
                summaryRecord.HideLinksWhenPreviewAvailable,
                localizationService.GetString("RecordList.DaySummaryLabel"),
                false,
                summaryRecord.CreatedUtc,
                images,
                audios,
                videoPreviewItems,
                overflowVideoLinks),

            _ => throw new InvalidOperationException($"Неподдерживаемый тип записи: {record.GetType().Name}.")
        };
    }

    private static string GetEventStatusKey(EventStatus status)
    {
        return status switch
        {
            Taskloom.Domain.EventStatus.Scheduled => "EventStatus.Scheduled",
            Taskloom.Domain.EventStatus.Completed => "EventStatus.Completed",
            Taskloom.Domain.EventStatus.Rescheduled => "EventStatus.Rescheduled",
            Taskloom.Domain.EventStatus.Canceled => "EventStatus.Canceled",
            _ => throw new InvalidOperationException($"Неподдерживаемый статус события: {status}.")
        };
    }
}
