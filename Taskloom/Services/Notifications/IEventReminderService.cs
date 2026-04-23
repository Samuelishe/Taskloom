namespace Taskloom.Services.Notifications;

/// <summary>
/// Описывает сервис напоминаний о событиях.
/// </summary>
public interface IEventReminderService : IDisposable
{
    /// <summary>
    /// Запускает периодическую проверку ближайших событий.
    /// </summary>
    void Start();

    /// <summary>
    /// Останавливает периодическую проверку ближайших событий.
    /// </summary>
    void Stop();
}
