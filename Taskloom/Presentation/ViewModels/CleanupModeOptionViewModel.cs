using CommunityToolkit.Mvvm.ComponentModel;
using Taskloom.Services.Localization;
using Taskloom.Services.Settings;

namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// Вариант политики очистки старых записей в настройках.
/// </summary>
public sealed class CleanupModeOptionViewModel : ObservableObject
{
    private readonly ILocalizationService _localizationService;
    private readonly string _titleKey;

    public CleanupModeOptionViewModel(
        ILocalizationService localizationService,
        string titleKey,
        RecordCleanupMode cleanupMode)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        ArgumentException.ThrowIfNullOrWhiteSpace(titleKey);

        _titleKey = titleKey;
        CleanupMode = cleanupMode;

        _localizationService.LanguageChanged += OnLanguageChanged;
    }

    public RecordCleanupMode CleanupMode { get; }

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
