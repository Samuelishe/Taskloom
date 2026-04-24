using Taskloom.Domain;
using Taskloom.Services.Settings;

namespace Taskloom.Services.Records;

/// <summary>
/// Описывает прикладные сценарии работы с календарными записями.
/// </summary>
public interface ICalendarRecordService
{
    /// <summary>
    /// Возвращает записи выбранной даты с необязательной фильтрацией по типу.
    /// </summary>
    Task<IReadOnlyList<CalendarRecord>> GetRecordsByDateAsync(
        DateOnly date,
        RecordType? type = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает черновик записи для редактирования.
    /// </summary>
    Task<CalendarRecordDraft?> GetDraftByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Создаёт или обновляет запись по черновику.
    /// </summary>
    Task<CalendarRecord> SaveAsync(CalendarRecordDraft draft, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет запись по идентификатору.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Переключает статус выполнения задачи.
    /// </summary>
    Task<TaskRecord> ToggleTaskCompletionAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполняет автоматическую очистку старых записей по выбранной политике.
    /// </summary>
    Task<int> CleanupOldRecordsAsync(RecordCleanupMode cleanupMode, CancellationToken cancellationToken = default);
}
