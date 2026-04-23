using CommunityToolkit.Mvvm.ComponentModel;
using Taskloom.Services.Localization;
using Taskloom.Services.Theming;

namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// Вариант темы оформления для окна настроек.
/// </summary>
public sealed class ThemeOptionViewModel : ObservableObject
{
    private readonly ILocalizationService _localizationService;
    private readonly string _titleKey;

    public ThemeOptionViewModel(
        ILocalizationService localizationService,
        ThemeDefinition themeDefinition)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        ArgumentNullException.ThrowIfNull(themeDefinition);

        ThemeId = themeDefinition.ThemeId;
        _titleKey = themeDefinition.TitleKey;

        _localizationService.LanguageChanged += OnLanguageChanged;
    }

    public string ThemeId { get; }

    public string Title => _localizationService.GetString(_titleKey);

    public override string ToString()
    {
        return Title;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Title));
    }
}
