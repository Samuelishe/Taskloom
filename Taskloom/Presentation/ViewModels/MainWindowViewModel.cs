using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Taskloom.Domain;
using Taskloom.Services.Localization;
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
    private bool _isInitialized;

    public MainWindowViewModel(
        ICalendarRecordService recordService,
        ILocalizationService localizationService,
        IAppSettingsService settingsService)
    {
        _recordService = recordService ?? throw new ArgumentNullException(nameof(recordService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        Records = new ObservableCollection<RecordListItemViewModel>();
        FilterOptions = new ObservableCollection<RecordTypeFilterOptionViewModel>();

        LoadRecordsCommand = new AsyncRelayCommand(LoadRecordsAsync);
        OpenCreateRecordCommand = new RelayCommand(OpenCreateRecord);
        OpenEditRecordCommand = new AsyncRelayCommand(OpenEditRecordAsync, CanOpenEditRecord);
        DeleteRecordCommand = new AsyncRelayCommand(DeleteSelectedRecordAsync, CanDeleteSelectedRecord);
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

    /// <summary>
    /// Выполняет начальную загрузку данных окна.
    /// </summary>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return LoadRecordsAsync(cancellationToken);
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
    }

    partial void OnIsBusyChanged(bool value)
    {
        OpenEditRecordCommand.NotifyCanExecuteChanged();
        DeleteRecordCommand.NotifyCanExecuteChanged();
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

            Records.Clear();

            foreach (var record in records)
            {
                Records.Add(RecordListItemViewModel.Create(record, _localizationService));
            }

            StatusText = Records.Count == 0
                ? _localizationService.GetString("MainWindow.Status.NoRecords")
                : _localizationService.Format("MainWindow.Status.RecordsFound", Records.Count);

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
            _localizationService);
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
            _localizationService);
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
        ActiveSettings = new SettingsViewModel(_localizationService, _settingsService);
        StatusText = _localizationService.GetString("MainWindow.Status.SettingsOpened");
    }

    public void CloseSettings()
    {
        ActiveSettings = null;
        StatusText = _localizationService.GetString("MainWindow.Status.SettingsClosed");
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(WindowTitle));
        SelectedDateDisplay = SelectedDate.ToString("dd MMMM yyyy");
        UpdateFilterOptions();
        _ = LoadRecordsCommand.ExecuteAsync(null);
    }
}
