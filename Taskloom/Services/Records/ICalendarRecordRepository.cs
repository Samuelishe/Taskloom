using Taskloom.Domain;

namespace Taskloom.Services.Records;

/// <summary>
/// Описывает доступ к календарным записям.
/// </summary>
public interface ICalendarRecordRepository
{
    /// <summary>
    /// Возвращает запись по идентификатору.
    /// </summary>
    Task<CalendarRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает записи выбранной даты с необязательной фильтрацией по типу.
    /// </summary>
    Task<IReadOnlyList<CalendarRecord>> GetByDateAsync(
        DateOnly date,
        RecordType? type = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохраняет запись.
    /// </summary>
    Task SaveAsync(CalendarRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет запись по идентификатору.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
