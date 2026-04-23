using System.Windows;
using Taskloom.Services.Theming;

namespace Taskloom.Infrastructure.Theming;

/// <summary>
/// Применяет темы интерфейса через WPF ResourceDictionary.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private readonly Application _application;
    private ResourceDictionary? _activeThemeDictionary;

    public ThemeService(Application application)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
    }

    public string CurrentThemeId { get; private set; } = ThemeIds.WarmLight;

    public IReadOnlyList<ThemeDefinition> Themes { get; } =
    [
        new ThemeDefinition(ThemeIds.WarmLight, "Theme.WarmLight", "Assets/Themes/WarmLightTheme.xaml"),
        new ThemeDefinition(ThemeIds.NeutralLight, "Theme.NeutralLight", "Assets/Themes/NeutralLightTheme.xaml")
    ];

    public void ApplyTheme(string themeId)
    {
        var theme = Themes.FirstOrDefault(item =>
                        string.Equals(item.ThemeId, themeId, StringComparison.OrdinalIgnoreCase))
                    ?? Themes.First(item => item.ThemeId == ThemeIds.WarmLight);

        var dictionary = new ResourceDictionary
        {
            Source = new Uri(theme.ResourcePath, UriKind.Relative)
        };

        if (_activeThemeDictionary is not null)
        {
            _application.Resources.MergedDictionaries.Remove(_activeThemeDictionary);
        }

        _application.Resources.MergedDictionaries.Insert(0, dictionary);
        _activeThemeDictionary = dictionary;
        CurrentThemeId = theme.ThemeId;
    }
}
