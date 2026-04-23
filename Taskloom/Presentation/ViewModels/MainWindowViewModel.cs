using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Taskloom.Domain;
using Taskloom.Services.Records;

namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// ViewModel главного окна с календарной навигацией и списком записей выбранного дня.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly ICalendarRecordService _recordService;
    private bool _isInitialized;

    public MainWindowViewModel(ICalendarRecordService recordService)
    {
        _recordService = recordService ?? throw new ArgumentNullException(nameof(recordService));

        WindowTitle = "Taskloom";
        Records = new ObservableCollection<RecordListItemViewModel>();
        FilterOptions = CreateFilterOptions();

        LoadRecordsCommand = new AsyncRelayCommand(LoadRecordsAsync);
        OpenCreateRecordCommand = new RelayCommand(OpenCreateRecord);
        OpenEditRecordCommand = new AsyncRelayCommand(OpenEditRecordAsync, CanOpenEditRecord);
        DeleteRecordCommand = new AsyncRelayCommand(DeleteSelectedRecordAsync, CanDeleteSelectedRecord);
        PreviousDayCommand = new RelayCommand(MoveToPreviousDay);
        NextDayCommand = new RelayCommand(MoveToNextDay);
        RefreshCommand = new AsyncRelayCommand(LoadRecordsAsync);

        SelectedDate = DateOnly.FromDateTime(DateTime.Today);
        SelectedFilter = FilterOptions[0];
        _isInitialized = true;
    }

    public string WindowTitle { get; }

    public ObservableCollection<RecordListItemViewModel> Records { get; }

    public IReadOnlyList<RecordTypeFilterOptionViewModel> FilterOptions { get; }

    public IAsyncRelayCommand LoadRecordsCommand { get; }

    public IRelayCommand OpenCreateRecordCommand { get; }

    public IAsyncRelayCommand OpenEditRecordCommand { get; }

    public IAsyncRelayCommand DeleteRecordCommand { get; }

    public IRelayCommand PreviousDayCommand { get; }

    public IRelayCommand NextDayCommand { get; }

    public IAsyncRelayCommand RefreshCommand { get; }

    [ObservableProperty]
    private DateOnly selectedDate;

    [ObservableProperty]
    private RecordTypeFilterOptionViewModel selectedFilter;

    [ObservableProperty]
    private RecordListItemViewModel? selectedRecord;

    [ObservableProperty]
    private RecordEditorViewModel? activeEditor;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusText = "Загрузка записей не выполнялась.";

    [ObservableProperty]
    private string selectedDateDisplay = string.Empty;

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
                Records.Add(RecordListItemViewModel.Create(record));
            }

            StatusText = Records.Count == 0
                ? "На выбранную дату записей нет."
                : $"Найдено записей: {Records.Count}.";

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
        ActiveEditor = RecordEditorViewModel.CreateNew(SelectedDate);
        StatusText = "Подготовлен черновик новой записи.";
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
            StatusText = "Не удалось найти запись для редактирования.";
            return;
        }

        ActiveEditor = RecordEditorViewModel.FromDraft(draft);
        StatusText = "Подготовлен черновик выбранной записи.";
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
        StatusText = "Запись удалена.";
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

    private static IReadOnlyList<RecordTypeFilterOptionViewModel> CreateFilterOptions()
    {
        return
        [
            new RecordTypeFilterOptionViewModel("Все записи", null),
            new RecordTypeFilterOptionViewModel("Tasks", RecordType.Task),
            new RecordTypeFilterOptionViewModel("Notes", RecordType.Note),
            new RecordTypeFilterOptionViewModel("Events", RecordType.Event),
            new RecordTypeFilterOptionViewModel("Day summaries", RecordType.DaySummary)
        ];
    }
}
