namespace Taskloom.Services.Theming;

/// <summary>
/// Управляет доступными темами и применением текущей темы интерфейса.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Идентификатор текущей активной темы.
    /// </summary>
    string CurrentThemeId { get; }

    /// <summary>
    /// Доступные темы интерфейса.
    /// </summary>
    IReadOnlyList<ThemeDefinition> Themes { get; }

    /// <summary>
    /// Применяет тему по её идентификатору.
    /// </summary>
    void ApplyTheme(string themeId);
}
