namespace Taskloom.Domain;

/// <summary>
/// Задача, привязанная к конкретной дате.
/// </summary>
public sealed class TaskRecord : CalendarRecord
{
    public TaskRecord(
        Guid id,
        DateOnly date,
        string title,
        string? details,
        bool isCompleted = false,
        TimeOnly? reminderTime = null,
        int sortOrder = 0,
        bool hideLinksWhenPreviewAvailable = false,
        DateTime? createdUtc = null)
        : base(id, RecordType.Task, date, title, details, sortOrder, hideLinksWhenPreviewAvailable, createdUtc)
    {
        IsCompleted = isCompleted;
        ReminderTime = reminderTime;
    }

    public bool IsCompleted { get; private set; }

    public TimeOnly? ReminderTime { get; private set; }

    public void MarkCompleted()
    {
        IsCompleted = true;
    }

    public void MarkPending()
    {
        IsCompleted = false;
    }

    public void SetReminderTime(TimeOnly reminderTime)
    {
        ReminderTime = reminderTime;
    }

    public void ClearReminder()
    {
        ReminderTime = null;
    }
}
