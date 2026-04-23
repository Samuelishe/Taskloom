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
        bool isCompleted)
    {
        Id = id;
        Type = type;
        TypeDisplayName = typeDisplayName;
        Title = title;
        Details = details;
        TimeDisplay = timeDisplay;
        IsCompleted = isCompleted;
    }

    public Guid Id { get; }

    public RecordType Type { get; }

    public string TypeDisplayName { get; }

    public string Title { get; }

    public string? Details { get; }

    public string TimeDisplay { get; }

    public bool IsCompleted { get; }

    public bool IsTask => Type == RecordType.Task;

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
                taskRecord.IsCompleted
                    ? localizationService.GetString("RecordList.TaskCompleted")
                    : localizationService.GetString("RecordList.TaskPending"),
                taskRecord.IsCompleted),

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
                false),

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
}
