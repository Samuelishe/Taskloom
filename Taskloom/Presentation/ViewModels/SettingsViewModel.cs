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
    private bool _isApplyingSettings;
    private bool _isInitialized;
    private bool _isRefreshingSelections;
    private bool _hasPendingSettingsApply;
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
            new LanguageOptionViewModel("en-US", "English"),
            new LanguageOptionViewModel("zh-CN", "简体中文")
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

        if (_isInitialized && !_isRefreshingSelections)
        {
            QueueSettingsApply();
        }
    }

    partial void OnSelectedThemeChanged(ThemeOptionViewModel value)
    {
        StatusText = null;

        if (_isInitialized && !_isRefreshingSelections)
        {
            QueueSettingsApply();
        }
    }

    partial void OnSelectedCleanupModeChanged(CleanupModeOptionViewModel value)
    {
        StatusText = null;

        if (_isInitialized && !_isRefreshingSelections)
        {
            QueueSettingsApply();
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

    private void QueueSettingsApply()
    {
        _hasPendingSettingsApply = true;

        if (_isApplyingSettings)
        {
            return;
        }

        _ = ApplyPendingSettingsAsync();
    }

    private async Task ApplyPendingSettingsAsync(CancellationToken cancellationToken = default)
    {
        if (_isApplyingSettings)
        {
            return;
        }

        _isApplyingSettings = true;
        IsApplying = true;

        try
        {
            while (_hasPendingSettingsApply)
            {
                _hasPendingSettingsApply = false;
                await ApplyCurrentSelectionsAsync(cancellationToken);
            }
        }
        catch (Exception exception)
        {
            StatusText = _localizationService.Format("Settings.SaveFailed", exception.Message);
        }
        finally
        {
            IsApplying = false;
            _isApplyingSettings = false;
        }
    }

    private async Task ApplyCurrentSelectionsAsync(CancellationToken cancellationToken)
    {
        if (SelectedLanguage is null || SelectedTheme is null || SelectedCleanupMode is null)
        {
            return;
        }

        var selectedLanguageCultureName = SelectedLanguage.CultureName;
        var selectedThemeId = SelectedTheme.ThemeId;
        var selectedCleanupMode = SelectedCleanupMode.CleanupMode;

        var settings = await _settingsService.LoadAsync(cancellationToken);
        settings.LanguageCultureName = selectedLanguageCultureName;
        settings.ThemeId = selectedThemeId;
        settings.RecordCleanupMode = selectedCleanupMode;

        await _settingsService.SaveAsync(settings, cancellationToken);

        if (!string.Equals(_localizationService.CurrentCultureName, selectedLanguageCultureName, StringComparison.OrdinalIgnoreCase))
        {
            await _localizationService.SetCultureAsync(selectedLanguageCultureName, cancellationToken);
        }

        if (!string.Equals(_themeService.CurrentThemeId, selectedThemeId, StringComparison.OrdinalIgnoreCase))
        {
            _themeService.ApplyTheme(selectedThemeId);
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        var selectedCleanupModeValue = SelectedCleanupMode?.CleanupMode ?? RecordCleanupMode.Never;
        var selectedThemeId = SelectedTheme?.ThemeId ?? _themeService.CurrentThemeId;

        _isRefreshingSelections = true;

        try
        {
            RebuildLocalizedOptions();

            SelectedLanguage = Languages.First(option =>
                string.Equals(option.CultureName, _localizationService.CurrentCultureName, StringComparison.OrdinalIgnoreCase));
            SelectedTheme = Themes.First(option => option.ThemeId == selectedThemeId);
            SelectedCleanupMode = CleanupModes.First(option => option.CleanupMode == selectedCleanupModeValue);
        }
        finally
        {
            _isRefreshingSelections = false;
        }
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
