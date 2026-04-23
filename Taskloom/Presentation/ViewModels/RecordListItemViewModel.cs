using Taskloom.Domain;
using Taskloom.Services.Localization;

namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// Модель элемента списка записей выбранной даты.
/// </summary>
public sealed class RecordListItemViewModel
{
    private RecordListItemViewModel(
        Guid id,
        RecordType type,
        string typeDisplayName,
        string title,
        string? details,
        string timeDisplay,
        bool isCompleted,
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
        TimeDisplay = timeDisplay;
        IsCompleted = isCompleted;
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

    public string TimeDisplay { get; }

    public bool IsCompleted { get; }

    public bool IsTask => Type == RecordType.Task;

    public bool IsEvent => Type == RecordType.Event;

    public bool HasLocation => !string.IsNullOrWhiteSpace(LocationDisplay);

    public string? LocationDisplay { get; }

    public EventStatus? EventStatus { get; }

    public string? EventStatusText { get; }

    public string? TaskStatusText { get; }

    public string? TaskStatusIcon { get; }

    /// <summary>
    /// Создаёт presentation-модель элемента списка из доменной записи.
    /// </summary>
    public static RecordListItemViewModel Create(CalendarRecord record, ILocalizationService localizationService)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(localizationService);

        return record switch
        {
            TaskRecord taskRecord => new RecordListItemViewModel(
                taskRecord.Id,
                taskRecord.Type,
                localizationService.GetString("RecordType.Task"),
                taskRecord.Title,
                taskRecord.Details,
                localizationService.GetString("RecordList.TaskLabel"),
                taskRecord.IsCompleted,
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
                localizationService.GetString("RecordList.NoteAnyTime"),
                false),

            EventRecord eventRecord => new RecordListItemViewModel(
                eventRecord.Id,
                eventRecord.Type,
                localizationService.GetString("RecordType.Event"),
                eventRecord.Title,
                eventRecord.Details,
                $"{eventRecord.StartTime:HH\\:mm} - {eventRecord.EndTime:HH\\:mm}",
                false,
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
                localizationService.GetString("RecordList.DaySummaryLabel"),
                false),

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
