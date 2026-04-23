namespace Taskloom.Services.Localization;

/// <summary>
/// Описывает локализацию пользовательских строк приложения.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Возвращает код текущей культуры интерфейса.
    /// </summary>
    string CurrentCultureName { get; }

    /// <summary>
    /// Возвращает локализованную строку по ключу.
    /// </summary>
    string GetString(string key);

    /// <summary>
    /// Возвращает локализованную строку с форматированием.
    /// </summary>
    string Format(string key, params object[] args);

    /// <summary>
    /// Переключает язык интерфейса.
    /// </summary>
    Task SetCultureAsync(string cultureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Срабатывает после смены языка интерфейса.
    /// </summary>
    event EventHandler? LanguageChanged;
}
