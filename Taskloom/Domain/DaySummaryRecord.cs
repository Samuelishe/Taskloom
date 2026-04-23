namespace Taskloom.Domain;

/// <summary>
/// Итог дня с расширенным текстовым описанием.
/// </summary>
public sealed class DaySummaryRecord : CalendarRecord
{
    public DaySummaryRecord(Guid id, DateOnly date, string title, string? details)
        : base(id, RecordType.DaySummary, date, title, details)
    {
    }
}
