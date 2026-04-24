namespace Taskloom.Domain;

/// <summary>
/// Заметка, привязанная к дате.
/// </summary>
public sealed class NoteRecord : CalendarRecord
{
    public NoteRecord(
        Guid id,
        DateOnly date,
        string title,
        string? details,
        int sortOrder = 0,
        bool hideLinksWhenPreviewAvailable = false,
        DateTime? createdUtc = null)
        : base(id, RecordType.Note, date, title, details, sortOrder, hideLinksWhenPreviewAvailable, createdUtc)
    {
    }
}
