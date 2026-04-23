using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Taskloom.Infrastructure.Storage;
using Taskloom.Domain;
using Taskloom.Services.Localization;
using Taskloom.Services.Media;
using Taskloom.Services.Records;
using Taskloom.Services.Settings;

namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// ViewModel главного окна с календарной навигацией и списком записей выбранного дня.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly ICalendarRecordService _recordService;
    private readonly ILocalizationService _localizationService;
    private readonly IAppSettingsService _settingsService;
    private readonly IRecordImageStorageService _imageStorageService;
    private readonly IRecordAudioStorageService _audioStorageService;
    private readonly IAudioPlaybackService _audioPlaybackService;
    private readonly string _recordLoadLogPath = TaskloomPaths.GetRecordLoadLogPath();
    private bool _isInitialized;

    public MainWindowViewModel(
        ICalendarRecordService recordService,
        ILocalizationService localizationService,
        IAppSettingsService settingsService,
        IRecordImageStorageService imageStorageService,
        IRecordAudioStorageService audioStorageService,
        IAudioPlaybackService audioPlaybackService)
    {
        _recordService = recordService ?? throw new ArgumentNullException(nameof(recordService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _imageStorageService = imageStorageService ?? throw new ArgumentNullException(nameof(imageStorageService));
        _audioStorageService = audioStorageService ?? throw new ArgumentNullException(nameof(audioStorageService));
        _audioPlaybackService = audioPlaybackService ?? throw new ArgumentNullException(nameof(audioPlaybackService));

        Records = new ObservableCollection<RecordListItemViewModel>();
        FilterOptions = new ObservableCollection<RecordTypeFilterOptionViewModel>();

        LoadRecordsCommand = new AsyncRelayCommand(LoadRecordsAsync);
        OpenCreateRecordCommand = new RelayCommand(OpenCreateRecord);
        OpenEditRecordCommand = new AsyncRelayCommand(OpenEditRecordAsync, CanOpenEditRecord);
        DeleteRecordCommand = new AsyncRelayCommand(DeleteSelectedRecordAsync, CanDeleteSelectedRecord);
        ToggleTaskCompletionCommand = new AsyncRelayCommand<RecordListItemViewModel?>(ToggleTaskCompletionAsync, CanToggleTaskCompletion);
        OpenRecordImageCommand = new RelayCommand<RecordImageListItemViewModel?>(OpenRecordImage, CanOpenRecordImage);
        OpenRecordAudioCommand = new RelayCommand<RecordAudioListItemViewModel?>(OpenRecordAudio, CanOpenRecordAudio);
        ToggleAudioPlaybackCommand = new RelayCommand<RecordAudioListItemViewModel?>(ToggleAudioPlayback, CanToggleAudioPlayback);
        PreviousDayCommand = new RelayCommand(MoveToPreviousDay);
        NextDayCommand = new RelayCommand(MoveToNextDay);
        RefreshCommand = new AsyncRelayCommand(LoadRecordsAsync);
        OpenSettingsCommand = new RelayCommand(OpenSettings);

        _localizationService.LanguageChanged += OnLanguageChanged;

        SelectedDate = DateOnly.FromDateTime(DateTime.Today);
        UpdateFilterOptions();
        SelectedFilter = FilterOptions[0];
        StatusText = _localizationService.GetString("MainWindow.Status.LoadNotStarted");
        _isInitialized = true;
    }

    public string WindowTitle => _localizationService.GetString("App.Title");

    public ObservableCollection<RecordListItemViewModel> Records { get; }

    public ObservableCollection<RecordTypeFilterOptionViewModel> FilterOptions { get; }

    public IAsyncRelayCommand LoadRecordsCommand { get; }

    public IRelayCommand OpenCreateRecordCommand { get; }

    public IAsyncRelayCommand OpenEditRecordCommand { get; }

    public IAsyncRelayCommand DeleteRecordCommand { get; }

    public IAsyncRelayCommand<RecordListItemViewModel?> ToggleTaskCompletionCommand { get; }

    public IRelayCommand<RecordImageListItemViewModel?> OpenRecordImageCommand { get; }

    public IRelayCommand<RecordAudioListItemViewModel?> OpenRecordAudioCommand { get; }

    public IRelayCommand<RecordAudioListItemViewModel?> ToggleAudioPlaybackCommand { get; }

    public IRelayCommand PreviousDayCommand { get; }

    public IRelayCommand NextDayCommand { get; }

    public IAsyncRelayCommand RefreshCommand { get; }

    public IRelayCommand OpenSettingsCommand { get; }

    [ObservableProperty]
    private DateOnly selectedDate;

    [ObservableProperty]
    private RecordTypeFilterOptionViewModel selectedFilter;

    [ObservableProperty]
    private RecordListItemViewModel? selectedRecord;

    [ObservableProperty]
    private RecordEditorViewModel? activeEditor;

    [ObservableProperty]
    private SettingsViewModel? activeSettings;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusText = string.Empty;

    [ObservableProperty]
    private string selectedDateDisplay = string.Empty;

    public DateTime SelectedDateValue
    {
        get => SelectedDate.ToDateTime(TimeOnly.MinValue);
        set => SelectedDate = DateOnly.FromDateTime(value);
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return LoadRecordsAsync(cancellationToken);
    }

    public void SeekAudio(RecordAudioListItemViewModel? item, double seconds)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.Path))
        {
            return;
        }

        _audioPlaybackService.Seek(item.Path, TimeSpan.FromSeconds(seconds));
    }

    public void BeginSeekAudio(RecordAudioListItemViewModel? item)
    {
        item?.BeginSeek();
    }

    public void UpdateSeekAudioPreview(RecordAudioListItemViewModel? item, double seconds)
    {
        item?.UpdateSeekPreview(seconds);
    }

    public void EndSeekAudio(RecordAudioListItemViewModel? item)
    {
        item?.EndSeek();
    }

    partial void OnSelectedDateChanged(DateOnly value)
    {
        SelectedDateDisplay = value.ToString("dd MMMM yyyy");
        OnPropertyChanged(nameof(SelectedDateValue));

        if (_isInitialized)
        {
            _ = LoadRecordsCommand.ExecuteAsync(null);
        }
    }

    partial void OnSelectedFilterChanged(RecordTypeFilterOptionViewModel value)
    {
        if (_isInitialized)
        {
            _ = LoadRecordsCommand.ExecuteAsync(null);
        }
    }

    partial void OnSelectedRecordChanged(RecordListItemViewModel? value)
    {
        OpenEditRecordCommand.NotifyCanExecuteChanged();
        DeleteRecordCommand.NotifyCanExecuteChanged();
        ToggleTaskCompletionCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        OpenEditRecordCommand.NotifyCanExecuteChanged();
        DeleteRecordCommand.NotifyCanExecuteChanged();
        ToggleTaskCompletionCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadRecordsAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var records = await _recordService.GetRecordsByDateAsync(
                SelectedDate,
                SelectedFilter.RecordType,
                cancellationToken);

            ClearRecords();
            var skippedRecordsCount = 0;

            foreach (var record in records)
            {
                try
                {
                    Records.Add(RecordListItemViewModel.Create(
                        record,
                        _localizationService,
                        _imageStorageService,
                        _audioStorageService,
                        _audioPlaybackService));
                }
                catch (Exception exception)
                {
                    skippedRecordsCount++;
                    TaskloomDiagnosticLog.AppendException(
                        _recordLoadLogPath,
                        exception,
                        $"Failed to build record view model. RecordId={record.Id}, Type={record.Type}, Title='{record.Title}'");
                }
            }

            StatusText = Records.Count == 0
                ? _localizationService.GetString("MainWindow.Status.NoRecords")
                : _localizationService.Format("MainWindow.Status.RecordsFound", Records.Count);

            if (skippedRecordsCount > 0)
            {
                TaskloomDiagnosticLog.Append(
                    _recordLoadLogPath,
                    $"Skipped records during load: {skippedRecordsCount}. SelectedDate={SelectedDate:yyyy-MM-dd}.");
            }

            if (SelectedRecord is not null)
            {
                SelectedRecord = Records.FirstOrDefault(item => item.Id == SelectedRecord.Id);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OpenCreateRecord()
    {
        ActiveEditor = RecordEditorViewModel.CreateNew(
            SelectedDate,
            SaveEditorAsync,
            CloseEditor,
            _localizationService,
            _imageStorageService,
            _audioStorageService);
        StatusText = _localizationService.GetString("MainWindow.Status.NewRecordPrepared");
    }

    private async Task OpenEditRecordAsync()
    {
        if (SelectedRecord is null)
        {
            return;
        }

        var draft = await _recordService.GetDraftByIdAsync(SelectedRecord.Id);

        if (draft is null)
        {
            StatusText = _localizationService.GetString("MainWindow.Status.RecordNotFound");
            return;
        }

        ActiveEditor = RecordEditorViewModel.FromDraft(
            draft,
            SaveEditorAsync,
            CloseEditor,
            _localizationService,
            _imageStorageService,
            _audioStorageService);
        StatusText = _localizationService.GetString("MainWindow.Status.EditRecordPrepared");
    }

    private bool CanOpenEditRecord()
    {
        return SelectedRecord is not null && !IsBusy;
    }

    private async Task DeleteSelectedRecordAsync()
    {
        if (SelectedRecord is null)
        {
            return;
        }

        await _recordService.DeleteAsync(SelectedRecord.Id);
        StatusText = _localizationService.GetString("MainWindow.Status.RecordDeleted");
        await LoadRecordsAsync();
    }

    private bool CanDeleteSelectedRecord()
    {
        return SelectedRecord is not null && !IsBusy;
    }

    private async Task ToggleTaskCompletionAsync(RecordListItemViewModel? item, CancellationToken cancellationToken)
    {
        if (item is null || !item.IsTask)
        {
            return;
        }

        var updatedTask = await _recordService.ToggleTaskCompletionAsync(item.Id, cancellationToken);
        StatusText = _localizationService.Format("MainWindow.Status.TaskStatusChanged", updatedTask.Title);

        await LoadRecordsAsync(cancellationToken);
        SelectedRecord = Records.FirstOrDefault(record => record.Id == updatedTask.Id);
    }

    private bool CanToggleTaskCompletion(RecordListItemViewModel? item)
    {
        return item is { IsTask: true } && !IsBusy;
    }

    private void OpenRecordImage(RecordImageListItemViewModel? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.Path))
        {
            return;
        }

        if (!File.Exists(item.Path))
        {
            StatusText = _localizationService.GetString("MainWindow.Status.ImageMissing");
            return;
        }

        Process.Start(new ProcessStartInfo(item.Path)
        {
            UseShellExecute = true
        });

        StatusText = _localizationService.Format("MainWindow.Status.ImageOpened", item.FileName);
    }

    private static bool CanOpenRecordImage(RecordImageListItemViewModel? item)
    {
        return item is not null && !string.IsNullOrWhiteSpace(item.Path);
    }

    private void OpenRecordAudio(RecordAudioListItemViewModel? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.Path))
        {
            return;
        }

        if (!File.Exists(item.Path))
        {
            StatusText = _localizationService.GetString("MainWindow.Status.AudioMissing");
            return;
        }

        Process.Start(new ProcessStartInfo(item.Path)
        {
            UseShellExecute = true
        });

        StatusText = _localizationService.Format("MainWindow.Status.AudioOpened", item.DisplayTitle);
    }

    private static bool CanOpenRecordAudio(RecordAudioListItemViewModel? item)
    {
        return item is not null && !string.IsNullOrWhiteSpace(item.Path);
    }

    private void ToggleAudioPlayback(RecordAudioListItemViewModel? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.Path))
        {
            return;
        }

        if (!File.Exists(item.Path))
        {
            StatusText = _localizationService.GetString("MainWindow.Status.AudioMissing");
            return;
        }

        _audioPlaybackService.TogglePlayback(item.Path);
        StatusText = _localizationService.Format("MainWindow.Status.AudioPlaybackChanged", item.DisplayTitle);
    }

    private static bool CanToggleAudioPlayback(RecordAudioListItemViewModel? item)
    {
        return item is not null && !string.IsNullOrWhiteSpace(item.Path);
    }

    private void MoveToPreviousDay()
    {
        SelectedDate = SelectedDate.AddDays(-1);
    }

    private void MoveToNextDay()
    {
        SelectedDate = SelectedDate.AddDays(1);
    }

    private void UpdateFilterOptions()
    {
        var currentType = SelectedFilter?.RecordType;

        FilterOptions.Clear();
        FilterOptions.Add(new RecordTypeFilterOptionViewModel(_localizationService, "Filter.All", null));
        FilterOptions.Add(new RecordTypeFilterOptionViewModel(_localizationService, "RecordType.Task", RecordType.Task));
        FilterOptions.Add(new RecordTypeFilterOptionViewModel(_localizationService, "RecordType.Note", RecordType.Note));
        FilterOptions.Add(new RecordTypeFilterOptionViewModel(_localizationService, "RecordType.Event", RecordType.Event));
        FilterOptions.Add(new RecordTypeFilterOptionViewModel(_localizationService, "RecordType.DaySummary", RecordType.DaySummary));

        SelectedFilter = FilterOptions.FirstOrDefault(option => option.RecordType == currentType) ?? FilterOptions[0];
    }

    private async Task SaveEditorAsync(RecordEditorViewModel editor, CancellationToken cancellationToken)
    {
        var savedRecord = await _recordService.SaveAsync(editor.ToDraft(), cancellationToken);

        ActiveEditor = null;
        StatusText = _localizationService.Format("MainWindow.Status.RecordSaved", savedRecord.Title);

        await LoadRecordsAsync(cancellationToken);
        SelectedRecord = Records.FirstOrDefault(item => item.Id == savedRecord.Id);
    }

    private void CloseEditor()
    {
        ActiveEditor = null;
        StatusText = _localizationService.GetString("MainWindow.Status.EditorClosed");
    }

    private void OpenSettings()
    {
        ActiveSettings = new SettingsViewModel(_localizationService, _settingsService, App.CurrentApp.ThemeService);
        StatusText = _localizationService.GetString("MainWindow.Status.SettingsOpened");
    }

    public void CloseSettings()
    {
        ActiveSettings = null;
        StatusText = _localizationService.GetString("MainWindow.Status.SettingsClosed");
    }

    private void ClearRecords()
    {
        foreach (var item in Records)
        {
            item.Dispose();
        }

        Records.Clear();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(WindowTitle));
        SelectedDateDisplay = SelectedDate.ToString("dd MMMM yyyy");
        UpdateFilterOptions();
        _ = LoadRecordsCommand.ExecuteAsync(null);
    }
}
