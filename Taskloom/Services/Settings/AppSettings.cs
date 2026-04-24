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

    /// <summary>
    /// Идентификатор активной темы оформления.
    /// </summary>
    public string ThemeId { get; set; } = "warm-light";

    /// <summary>
    /// Политика автоматической очистки старых записей при запуске.
    /// </summary>
    public RecordCleanupMode RecordCleanupMode { get; set; } = RecordCleanupMode.Never;

    /// <summary>
    /// Признак сохранённых параметров главного окна.
    /// </summary>
    public bool HasMainWindowPlacement { get; set; }

    /// <summary>
    /// Последняя ширина главного окна в обычном состоянии.
    /// </summary>
    public double MainWindowWidth { get; set; } = 1460;

    /// <summary>
    /// Последняя высота главного окна в обычном состоянии.
    /// </summary>
    public double MainWindowHeight { get; set; } = 920;

    /// <summary>
    /// Последнее состояние главного окна.
    /// </summary>
    public string MainWindowState { get; set; } = "Maximized";
}
