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
        bool isCompleted = false)
        : base(id, RecordType.Task, date, title, details)
    {
        IsCompleted = isCompleted;
    }

    public bool IsCompleted { get; private set; }

    public void MarkCompleted()
    {
        IsCompleted = true;
    }

    public void MarkPending()
    {
        IsCompleted = false;
    }
}
