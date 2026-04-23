using System.Windows;
using Taskloom.Services.Theming;

namespace Taskloom.Infrastructure.Theming;

/// <summary>
/// Применяет темы интерфейса через WPF ResourceDictionary.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private readonly System.Windows.Application _application;
    private ResourceDictionary? _activeThemeDictionary;

    public ThemeService(System.Windows.Application application)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
    }

    public string CurrentThemeId { get; private set; } = ThemeIds.WarmLight;

    public IReadOnlyList<ThemeDefinition> Themes { get; } =
    [
        new ThemeDefinition(ThemeIds.WarmLight, "Theme.WarmLight", "Assets/Themes/WarmLightTheme.xaml"),
        new ThemeDefinition(ThemeIds.NeutralLight, "Theme.NeutralLight", "Assets/Themes/NeutralLightTheme.xaml"),
        new ThemeDefinition(ThemeIds.GrayLight, "Theme.GrayLight", "Assets/Themes/GrayLightTheme.xaml"),
        new ThemeDefinition(ThemeIds.SoftDark, "Theme.SoftDark", "Assets/Themes/SoftDarkTheme.xaml"),
        new ThemeDefinition(ThemeIds.DeepDark, "Theme.DeepDark", "Assets/Themes/DeepDarkTheme.xaml"),
        new ThemeDefinition(ThemeIds.Windows11, "Theme.Windows11", "Assets/Themes/Windows11Theme.xaml"),
        new ThemeDefinition(ThemeIds.Ubuntu, "Theme.Ubuntu", "Assets/Themes/UbuntuTheme.xaml"),
        new ThemeDefinition(ThemeIds.Windows7, "Theme.Windows7", "Assets/Themes/Windows7Theme.xaml"),
        new ThemeDefinition(ThemeIds.MacOs, "Theme.MacOs", "Assets/Themes/MacOsTheme.xaml"),
        new ThemeDefinition(ThemeIds.FrogGreen, "Theme.FrogGreen", "Assets/Themes/FrogGreenTheme.xaml"),
        new ThemeDefinition(ThemeIds.VolcanicFire, "Theme.VolcanicFire", "Assets/Themes/VolcanicFireTheme.xaml"),
        new ThemeDefinition(ThemeIds.CosmicCold, "Theme.CosmicCold", "Assets/Themes/CosmicColdTheme.xaml"),
        new ThemeDefinition(ThemeIds.SnowWhite, "Theme.SnowWhite", "Assets/Themes/SnowWhiteTheme.xaml")
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
