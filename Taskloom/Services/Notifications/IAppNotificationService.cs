namespace Taskloom.Services.Notifications;

/// <summary>
/// Описывает транспорт локальных уведомлений приложения.
/// </summary>
public interface IAppNotificationService : IDisposable
{
    /// <summary>
    /// Показывает информационное уведомление.
    /// </summary>
    void ShowInfo(string title, string message);

    /// <summary>
    /// Показывает предупреждающее уведомление.
    /// </summary>
    void ShowWarning(string title, string message);
}
