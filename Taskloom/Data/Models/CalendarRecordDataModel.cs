namespace Taskloom.Data.Models;

/// <summary>
/// Модель хранения календарной записи для SQLite.
/// </summary>
public sealed class CalendarRecordDataModel
{
    /// <summary>
    /// Идентификатор записи в строковом виде.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Числовой код типа записи.
    /// </summary>
    public int TypeId { get; set; }

    /// <summary>
    /// Дата записи в формате ISO 8601.
    /// </summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// Заголовок записи.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Расширенное описание записи.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Момент создания записи в формате ISO 8601 UTC.
    /// </summary>
    public string CreatedUtc { get; set; } = string.Empty;

    /// <summary>
    /// Признак завершения задачи.
    /// </summary>
    public bool? IsCompleted { get; set; }

    /// <summary>
    /// Время напоминания о задаче в формате HH:mm.
    /// </summary>
    public string? TaskReminderTime { get; set; }

    /// <summary>
    /// Время начала события в формате HH:mm.
    /// </summary>
    public string? StartTime { get; set; }

    /// <summary>
    /// Время окончания события в формате HH:mm.
    /// </summary>
    public string? EndTime { get; set; }

    /// <summary>
    /// Место проведения события.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Числовой код статуса события.
    /// </summary>
    public int? EventStatusId { get; set; }

    /// <summary>
    /// Время напоминания до события в минутах.
    /// </summary>
    public int? ReminderMinutesBefore { get; set; }
}
