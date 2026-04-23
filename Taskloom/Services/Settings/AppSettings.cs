namespace Taskloom.Services.Settings;

/// <summary>
/// Настройки приложения, не относящиеся к предметным данным.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Код языка интерфейса.
    /// </summary>
    public string LanguageCultureName { get; set; } = "ru-RU";
}
