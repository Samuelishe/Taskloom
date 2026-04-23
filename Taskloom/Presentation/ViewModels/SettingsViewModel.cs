using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Taskloom.Services.Localization;
using Taskloom.Services.Settings;
using Taskloom.Services.Theming;

namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// ViewModel окна настроек приложения.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ILocalizationService _localizationService;
    private readonly IAppSettingsService _settingsService;
    private readonly IThemeService _themeService;
    private bool _isInitialized;

    public SettingsViewModel(
        ILocalizationService localizationService,
        IAppSettingsService settingsService,
        IThemeService themeService)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

        Languages =
        [
            new LanguageOptionViewModel("ru-RU", "Русский"),
            new LanguageOptionViewModel("en-US", "English")
        ];

        Themes = _themeService.Themes
            .Select(theme => new ThemeOptionViewModel(_localizationService, theme))
            .ToArray();

        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        CancelCommand = new RelayCommand(Cancel);

        SelectedLanguage = Languages.First(option =>
            string.Equals(option.CultureName, _localizationService.CurrentCultureName, StringComparison.OrdinalIgnoreCase));
        SelectedTheme = Themes.First(option =>
            string.Equals(option.ThemeId, _themeService.CurrentThemeId, StringComparison.OrdinalIgnoreCase));
        _isInitialized = true;
    }

    public IReadOnlyList<LanguageOptionViewModel> Languages { get; }

    public IReadOnlyList<ThemeOptionViewModel> Themes { get; }

    public IAsyncRelayCommand SaveCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public event EventHandler<SettingsCloseRequestedEventArgs>? CloseRequested;

    [ObservableProperty]
    private LanguageOptionViewModel selectedLanguage;

    [ObservableProperty]
    private ThemeOptionViewModel selectedTheme;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private string? statusText;

    partial void OnSelectedLanguageChanged(LanguageOptionViewModel value)
    {
        StatusText = null;

        if (_isInitialized)
        {
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    partial void OnSelectedThemeChanged(ThemeOptionViewModel value)
    {
        StatusText = null;

        if (_isInitialized)
        {
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    partial void OnIsSavingChanged(bool value)
    {
        SaveCommand.NotifyCanExecuteChanged();
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        IsSaving = true;

        try
        {
            var settings = await _settingsService.LoadAsync(cancellationToken);
            settings.LanguageCultureName = SelectedLanguage.CultureName;
            settings.ThemeId = SelectedTheme.ThemeId;

            await _settingsService.SaveAsync(settings, cancellationToken);
            await _localizationService.SetCultureAsync(SelectedLanguage.CultureName, cancellationToken);
            _themeService.ApplyTheme(SelectedTheme.ThemeId);

            CloseRequested?.Invoke(this, new SettingsCloseRequestedEventArgs(true));
        }
        catch (Exception exception)
        {
            StatusText = _localizationService.Format("Settings.SaveFailed", exception.Message);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool CanSave()
    {
        return !IsSaving &&
               (
                   !string.Equals(
                       SelectedLanguage.CultureName,
                       _localizationService.CurrentCultureName,
                       StringComparison.OrdinalIgnoreCase)
                   || !string.Equals(
                       SelectedTheme.ThemeId,
                       _themeService.CurrentThemeId,
                       StringComparison.OrdinalIgnoreCase)
               );
    }

    private void Cancel()
    {
        CloseRequested?.Invoke(this, new SettingsCloseRequestedEventArgs(false));
    }
}
