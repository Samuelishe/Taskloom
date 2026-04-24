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
    private IReadOnlyList<ThemeOptionViewModel> _themes = Array.Empty<ThemeOptionViewModel>();
    private IReadOnlyList<CleanupModeOptionViewModel> _cleanupModes = Array.Empty<CleanupModeOptionViewModel>();

    public SettingsViewModel(
        ILocalizationService localizationService,
        IAppSettingsService settingsService,
        IThemeService themeService)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _localizationService.LanguageChanged += OnLanguageChanged;

        Languages =
        [
            new LanguageOptionViewModel("ru-RU", "Русский"),
            new LanguageOptionViewModel("en-US", "English")
        ];
        RebuildLocalizedOptions();

        SelectedLanguage = Languages.First(option =>
            string.Equals(option.CultureName, _localizationService.CurrentCultureName, StringComparison.OrdinalIgnoreCase));
        SelectedTheme = Themes.First(option =>
            string.Equals(option.ThemeId, _themeService.CurrentThemeId, StringComparison.OrdinalIgnoreCase));
        SelectedCleanupMode = CleanupModes[0];
        _ = InitializeAsync();
    }

    public IReadOnlyList<LanguageOptionViewModel> Languages { get; }

    public IReadOnlyList<ThemeOptionViewModel> Themes
    {
        get => _themes;
        private set => SetProperty(ref _themes, value);
    }

    public IReadOnlyList<CleanupModeOptionViewModel> CleanupModes
    {
        get => _cleanupModes;
        private set => SetProperty(ref _cleanupModes, value);
    }

    public event EventHandler<SettingsCloseRequestedEventArgs>? CloseRequested;

    [ObservableProperty]
    private LanguageOptionViewModel selectedLanguage;

    [ObservableProperty]
    private ThemeOptionViewModel selectedTheme;

    [ObservableProperty]
    private CleanupModeOptionViewModel selectedCleanupMode;

    [ObservableProperty]
    private bool isApplying;

    [ObservableProperty]
    private string? statusText;

    partial void OnSelectedLanguageChanged(LanguageOptionViewModel value)
    {
        StatusText = null;

        if (_isInitialized)
        {
            _ = ApplyLanguageAndThemeAsync();
        }
    }

    partial void OnSelectedThemeChanged(ThemeOptionViewModel value)
    {
        StatusText = null;

        if (_isInitialized)
        {
            _ = ApplyLanguageAndThemeAsync();
        }
    }

    partial void OnSelectedCleanupModeChanged(CleanupModeOptionViewModel value)
    {
        StatusText = null;

        if (_isInitialized)
        {
            _ = ApplyCleanupModeAsync();
        }
    }

    public void Close()
    {
        CloseRequested?.Invoke(this, new SettingsCloseRequestedEventArgs(false));
    }

    public CleanupModeOptionViewModel GetCleanupModeOption(RecordCleanupMode cleanupMode)
    {
        return CleanupModes.First(option => option.CleanupMode == cleanupMode);
    }

    private async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _settingsService.LoadAsync(cancellationToken);
            SelectedCleanupMode = CleanupModes.First(option => option.CleanupMode == settings.RecordCleanupMode);
        }
        catch (Exception exception)
        {
            StatusText = _localizationService.Format("Settings.SaveFailed", exception.Message);
        }
        finally
        {
            _isInitialized = true;
        }
    }

    private async Task ApplyLanguageAndThemeAsync(CancellationToken cancellationToken = default)
    {
        if (IsApplying)
        {
            return;
        }

        IsApplying = true;

        try
        {
            var settings = await _settingsService.LoadAsync(cancellationToken);
            settings.LanguageCultureName = SelectedLanguage.CultureName;
            settings.ThemeId = SelectedTheme.ThemeId;
            settings.RecordCleanupMode = SelectedCleanupMode.CleanupMode;

            await _settingsService.SaveAsync(settings, cancellationToken);
            await _localizationService.SetCultureAsync(SelectedLanguage.CultureName, cancellationToken);
            _themeService.ApplyTheme(SelectedTheme.ThemeId);
        }
        catch (Exception exception)
        {
            StatusText = _localizationService.Format("Settings.SaveFailed", exception.Message);
        }
        finally
        {
            IsApplying = false;
        }
    }

    private async Task ApplyCleanupModeAsync(CancellationToken cancellationToken = default)
    {
        if (IsApplying)
        {
            return;
        }

        try
        {
            var settings = await _settingsService.LoadAsync(cancellationToken);
            settings.RecordCleanupMode = SelectedCleanupMode.CleanupMode;
            await _settingsService.SaveAsync(settings, cancellationToken);
        }
        catch (Exception exception)
        {
            StatusText = _localizationService.Format("Settings.SaveFailed", exception.Message);
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        var selectedThemeId = SelectedTheme.ThemeId;
        var selectedCleanupModeValue = SelectedCleanupMode.CleanupMode;

        RebuildLocalizedOptions();

        SelectedTheme = Themes.First(option => option.ThemeId == selectedThemeId);
        SelectedCleanupMode = CleanupModes.First(option => option.CleanupMode == selectedCleanupModeValue);
    }

    private void RebuildLocalizedOptions()
    {
        Themes = _themeService.Themes
            .Select(theme => new ThemeOptionViewModel(_localizationService, theme))
            .ToArray();

        CleanupModes =
        [
            new CleanupModeOptionViewModel(_localizationService, "Settings.Cleanup.Never", RecordCleanupMode.Never),
            new CleanupModeOptionViewModel(_localizationService, "Settings.Cleanup.All7Days", RecordCleanupMode.DeleteAllOlderThan7Days),
            new CleanupModeOptionViewModel(_localizationService, "Settings.Cleanup.All1Month", RecordCleanupMode.DeleteAllOlderThan1Month),
            new CleanupModeOptionViewModel(_localizationService, "Settings.Cleanup.CompletedAndPast7Days", RecordCleanupMode.DeleteCompletedAndPastOlderThan7Days),
            new CleanupModeOptionViewModel(_localizationService, "Settings.Cleanup.CompletedAndPast1Month", RecordCleanupMode.DeleteCompletedAndPastOlderThan1Month)
        ];
    }
}
