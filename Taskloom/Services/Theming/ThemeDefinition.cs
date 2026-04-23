namespace Taskloom.Services.Theming;

/// <summary>
/// Описывает доступную тему оформления приложения.
/// </summary>
public sealed class ThemeDefinition
{
    public ThemeDefinition(string themeId, string titleKey, string resourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(themeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(titleKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourcePath);

        ThemeId = themeId;
        TitleKey = titleKey;
        ResourcePath = resourcePath;
    }

    public string ThemeId { get; }

    public string TitleKey { get; }

    public string ResourcePath { get; }
}
