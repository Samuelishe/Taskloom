using Taskloom.Domain;

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

    /// <summary>
    /// Создаёт presentation-модель элемента списка из доменной записи.
    /// </summary>
    public static RecordListItemViewModel Create(CalendarRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return record switch
        {
            TaskRecord taskRecord => new RecordListItemViewModel(
                taskRecord.Id,
                taskRecord.Type,
                "Task",
                taskRecord.Title,
                taskRecord.Details,
                taskRecord.IsCompleted ? "Completed" : "Pending",
                taskRecord.IsCompleted),

            NoteRecord noteRecord => new RecordListItemViewModel(
                noteRecord.Id,
                noteRecord.Type,
                "Note",
                noteRecord.Title,
                noteRecord.Details,
                "Any time",
                false),

            EventRecord eventRecord => new RecordListItemViewModel(
                eventRecord.Id,
                eventRecord.Type,
                "Event",
                eventRecord.Title,
                eventRecord.Details,
                $"{eventRecord.StartTime:HH\\:mm} - {eventRecord.EndTime:HH\\:mm}",
                false),

            DaySummaryRecord summaryRecord => new RecordListItemViewModel(
                summaryRecord.Id,
                summaryRecord.Type,
                "Day summary",
                summaryRecord.Title,
                summaryRecord.Details,
                "Summary",
                false),

            _ => throw new InvalidOperationException($"Неподдерживаемый тип записи: {record.GetType().Name}.")
        };
    }
}
