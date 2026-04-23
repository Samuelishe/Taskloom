using Taskloom.Domain;

namespace Taskloom.Services.Records;

/// <summary>
/// Черновик записи для сценариев создания и редактирования.
/// </summary>
public sealed class CalendarRecordDraft
{
    /// <summary>
    /// Идентификатор записи. Для новой записи может отсутствовать.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Тип записи.
    /// </summary>
    public RecordType Type { get; set; }

    /// <summary>
    /// Дата записи.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Заголовок записи.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Расширенное описание записи.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Признак завершения задачи.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Время начала события.
    /// </summary>
    public TimeOnly? StartTime { get; set; }

    /// <summary>
    /// Время окончания события.
    /// </summary>
    public TimeOnly? EndTime { get; set; }

    /// <summary>
    /// Место проведения события.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Статус события.
    /// </summary>
    public EventStatus EventStatus { get; set; } = EventStatus.Scheduled;

    /// <summary>
    /// За сколько минут до события нужно показать напоминание.
    /// </summary>
    public int ReminderMinutesBefore { get; set; } = 60;
}
