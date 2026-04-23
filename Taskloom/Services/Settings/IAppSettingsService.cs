namespace Taskloom.Services.Settings;

/// <summary>
/// Описывает доступ к настройкам приложения.
/// </summary>
public interface IAppSettingsService
{
    /// <summary>
    /// Загружает настройки приложения.
    /// </summary>
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохраняет настройки приложения.
    /// </summary>
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
