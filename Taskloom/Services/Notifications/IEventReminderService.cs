namespace Taskloom.Services.Notifications;

/// <summary>
/// Описывает сервис напоминаний о календарных записях.
/// </summary>
public interface IEventReminderService : IDisposable
{
    /// <summary>
    /// Запускает периодическую проверку ближайших напоминаний.
    /// </summary>
    void Start();

    /// <summary>
    /// Останавливает периодическую проверку ближайших напоминаний.
    /// </summary>
    void Stop();
}
