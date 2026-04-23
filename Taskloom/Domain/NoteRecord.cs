namespace Taskloom.Domain;

/// <summary>
/// Заметка, привязанная к дате.
/// </summary>
public sealed class NoteRecord : CalendarRecord
{
    public NoteRecord(Guid id, DateOnly date, string title, string? details)
        : base(id, RecordType.Note, date, title, details)
    {
    }
}
