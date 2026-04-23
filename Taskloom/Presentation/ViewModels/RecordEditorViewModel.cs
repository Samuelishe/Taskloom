using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Taskloom.Domain;
using Taskloom.Services.Localization;
using Taskloom.Services.Records;

namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// ViewModel редактора записи.
/// </summary>
public partial class RecordEditorViewModel : ObservableObject
{
    private readonly Func<RecordEditorViewModel, CancellationToken, Task> _saveAsync;
    private readonly Action _cancel;
    private readonly ILocalizationService _localizationService;
    private bool _isTimePartSyncing;

    private RecordEditorViewModel(
        Func<RecordEditorViewModel, CancellationToken, Task> saveAsync,
        Action cancel,
        ILocalizationService localizationService)
    {
        _saveAsync = saveAsync ?? throw new ArgumentNullException(nameof(saveAsync));
        _cancel = cancel ?? throw new ArgumentNullException(nameof(cancel));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));

        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        CancelCommand = new RelayCommand(Cancel);
        RecordTypeOptions = new ObservableCollection<RecordTypeFilterOptionViewModel>
        {
            new(_localizationService, "RecordType.Task", RecordType.Task),
            new(_localizationService, "RecordType.Note", RecordType.Note),
            new(_localizationService, "RecordType.Event", RecordType.Event),
            new(_localizationService, "RecordType.DaySummary", RecordType.DaySummary)
        };
        HourOptions = CreateTimePartOptions(0, 23);
        MinuteSecondOptions = CreateTimePartOptions(0, 59);
        EventStatusOptions = CreateEventStatusOptions(_localizationService);
        ReminderOptions = CreateReminderOptions(_localizationService);

        _localizationService.LanguageChanged += OnLanguageChanged;
    }

    [ObservableProperty]
    private Guid? id;

    [ObservableProperty]
    private RecordType type;

    [ObservableProperty]
    private DateOnly date;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string? details;

    [ObservableProperty]
    private bool isCompleted;

    [ObservableProperty]
    private TimeOnly? startTime;

    [ObservableProperty]
    private TimeOnly? endTime;

    [ObservableProperty]
    private int selectedStartHour;

    [ObservableProperty]
    private int selectedStartMinute;

    [ObservableProperty]
    private int selectedStartSecond;

    [ObservableProperty]
    private int selectedEndHour;

    [ObservableProperty]
    private int selectedEndMinute;

    [ObservableProperty]
    private int selectedEndSecond;

    [ObservableProperty]
    private string? location;

    [ObservableProperty]
    private EventStatus eventStatus = EventStatus.Scheduled;

    [ObservableProperty]
    private int reminderMinutesBefore = 60;

    [ObservableProperty]
    private string editorTitle = string.Empty;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private string? validationMessage;

    public bool IsTask => Type == RecordType.Task;

    public bool IsEvent => Type == RecordType.Event;

    public bool IsNote => Type == RecordType.Note;

    public bool IsDaySummary => Type == RecordType.DaySummary;

    public DateTime DateValue
    {
        get => Date.ToDateTime(TimeOnly.MinValue);
        set => Date = DateOnly.FromDateTime(value);
    }

    public IAsyncRelayCommand SaveCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public ObservableCollection<RecordTypeFilterOptionViewModel> RecordTypeOptions { get; }

    public IReadOnlyList<TimePartOptionViewModel> HourOptions { get; }

    public IReadOnlyList<TimePartOptionViewModel> MinuteSecondOptions { get; }

    public ObservableCollection<EventStatusOptionViewModel> EventStatusOptions { get; }

    public ObservableCollection<ReminderOptionViewModel> ReminderOptions { get; }

    public event EventHandler<RecordEditorCloseRequestedEventArgs>? CloseRequested;

    /// <summary>
    /// Создаёт новый черновик записи для выбранной даты.
    /// </summary>
    public static RecordEditorViewModel CreateNew(
        DateOnly date,
        Func<RecordEditorViewModel, CancellationToken, Task> saveAsync,
        Action cancel,
        ILocalizationService localizationService)
    {
        return new RecordEditorViewModel(saveAsync, cancel, localizationService)
        {
            Type = RecordType.Task,
            Date = date,
            EditorTitle = localizationService.GetString("Editor.NewTitle")
        };
    }

    /// <summary>
    /// Создаёт ViewModel редактора из существующего черновика.
    /// </summary>
    public static RecordEditorViewModel FromDraft(
        CalendarRecordDraft draft,
        Func<RecordEditorViewModel, CancellationToken, Task> saveAsync,
        Action cancel,
        ILocalizationService localizationService)
    {
        ArgumentNullException.ThrowIfNull(draft);

        return new RecordEditorViewModel(saveAsync, cancel, localizationService)
        {
            Id = draft.Id,
            Type = draft.Type,
            Date = draft.Date,
            Title = draft.Title,
            Details = draft.Details,
            IsCompleted = draft.IsCompleted,
            StartTime = draft.StartTime,
            EndTime = draft.EndTime,
            Location = draft.Location,
            EventStatus = draft.EventStatus,
            ReminderMinutesBefore = draft.ReminderMinutesBefore,
            EditorTitle = localizationService.GetString("Editor.EditTitle")
        };
    }

    /// <summary>
    /// Возвращает черновик для сохранения.
    /// </summary>
    public CalendarRecordDraft ToDraft()
    {
        return new CalendarRecordDraft
        {
            Id = Id,
            Type = Type,
            Date = Date,
            Title = Title,
            Details = Details,
            IsCompleted = IsCompleted,
            StartTime = StartTime,
            EndTime = EndTime,
            Location = Location,
            EventStatus = EventStatus,
            ReminderMinutesBefore = ReminderMinutesBefore
        };
    }

    partial void OnTypeChanged(RecordType value)
    {
        OnPropertyChanged(nameof(IsTask));
        OnPropertyChanged(nameof(IsEvent));
        OnPropertyChanged(nameof(IsNote));
        OnPropertyChanged(nameof(IsDaySummary));
        SaveCommand.NotifyCanExecuteChanged();

        if (value != RecordType.Task)
        {
            IsCompleted = false;
        }

        if (value != RecordType.Event)
        {
            StartTime = null;
            EndTime = null;
            Location = null;
            EventStatus = Taskloom.Domain.EventStatus.Scheduled;
            ReminderMinutesBefore = 60;
        }

        if (value == RecordType.Event)
        {
            StartTime ??= new TimeOnly(9, 0, 0);
            EndTime ??= new TimeOnly(10, 0, 0);
            EventStatus = Taskloom.Domain.EventStatus.Scheduled;
            ReminderMinutesBefore = 60;
        }
    }

    partial void OnTitleChanged(string value)
    {
        ValidationMessage = null;
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnDateChanged(DateOnly value)
    {
        OnPropertyChanged(nameof(DateValue));
    }

    partial void OnStartTimeChanged(TimeOnly? value)
    {
        ValidationMessage = null;
        ApplyStartTimeToParts(value);
    }

    partial void OnEndTimeChanged(TimeOnly? value)
    {
        ValidationMessage = null;
        ApplyEndTimeToParts(value);
    }

    partial void OnSelectedStartHourChanged(int value)
    {
        UpdateStartTimeFromParts();
    }

    partial void OnSelectedStartMinuteChanged(int value)
    {
        UpdateStartTimeFromParts();
    }

    partial void OnSelectedStartSecondChanged(int value)
    {
        UpdateStartTimeFromParts();
    }

    partial void OnSelectedEndHourChanged(int value)
    {
        UpdateEndTimeFromParts();
    }

    partial void OnSelectedEndMinuteChanged(int value)
    {
        UpdateEndTimeFromParts();
    }

    partial void OnSelectedEndSecondChanged(int value)
    {
        UpdateEndTimeFromParts();
    }

    partial void OnIsSavingChanged(bool value)
    {
        SaveCommand.NotifyCanExecuteChanged();
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ValidationMessage = Validate();

        if (!string.IsNullOrWhiteSpace(ValidationMessage))
        {
            return;
        }

        IsSaving = true;

        try
        {
            await _saveAsync(this, cancellationToken);
            CloseRequested?.Invoke(this, new RecordEditorCloseRequestedEventArgs(true));
        }
        catch (Exception exception)
        {
            ValidationMessage = exception.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool CanSave()
    {
        return !IsSaving && !string.IsNullOrWhiteSpace(Title);
    }

    private void Cancel()
    {
        _cancel();
        CloseRequested?.Invoke(this, new RecordEditorCloseRequestedEventArgs(false));
    }

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            return _localizationService.GetString("Editor.Validation.TitleRequired");
        }

        if (!IsEvent)
        {
            return null;
        }

        if (StartTime is null || EndTime is null)
        {
            return _localizationService.GetString("Editor.Validation.EventTimesRequired");
        }

        if (EndTime <= StartTime)
        {
            return _localizationService.GetString("Editor.Validation.EventEndAfterStart");
        }

        return null;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        EditorTitle = Id.HasValue
            ? _localizationService.GetString("Editor.EditTitle")
            : _localizationService.GetString("Editor.NewTitle");
    }

    private void ApplyStartTimeToParts(TimeOnly? value)
    {
        if (value is null || _isTimePartSyncing)
        {
            return;
        }

        _isTimePartSyncing = true;

        try
        {
            SelectedStartHour = value.Value.Hour;
            SelectedStartMinute = value.Value.Minute;
            SelectedStartSecond = value.Value.Second;
        }
        finally
        {
            _isTimePartSyncing = false;
        }
    }

    private void ApplyEndTimeToParts(TimeOnly? value)
    {
        if (value is null || _isTimePartSyncing)
        {
            return;
        }

        _isTimePartSyncing = true;

        try
        {
            SelectedEndHour = value.Value.Hour;
            SelectedEndMinute = value.Value.Minute;
            SelectedEndSecond = value.Value.Second;
        }
        finally
        {
            _isTimePartSyncing = false;
        }
    }

    private void UpdateStartTimeFromParts()
    {
        if (_isTimePartSyncing || !IsEvent)
        {
            return;
        }

        StartTime = new TimeOnly(SelectedStartHour, SelectedStartMinute, SelectedStartSecond);
    }

    private void UpdateEndTimeFromParts()
    {
        if (_isTimePartSyncing || !IsEvent)
        {
            return;
        }

        EndTime = new TimeOnly(SelectedEndHour, SelectedEndMinute, SelectedEndSecond);
    }

    private static IReadOnlyList<TimePartOptionViewModel> CreateTimePartOptions(int minValue, int maxValue)
    {
        var options = new List<TimePartOptionViewModel>();

        for (var value = minValue; value <= maxValue; value++)
        {
            options.Add(new TimePartOptionViewModel(value));
        }

        return options;
    }

    private static ObservableCollection<EventStatusOptionViewModel> CreateEventStatusOptions(ILocalizationService localizationService)
    {
        return new ObservableCollection<EventStatusOptionViewModel>
        {
            new(localizationService, "EventStatus.Scheduled", EventStatus.Scheduled),
            new(localizationService, "EventStatus.Completed", EventStatus.Completed),
            new(localizationService, "EventStatus.Rescheduled", EventStatus.Rescheduled),
            new(localizationService, "EventStatus.Canceled", EventStatus.Canceled)
        };
    }

    private static ObservableCollection<ReminderOptionViewModel> CreateReminderOptions(ILocalizationService localizationService)
    {
        return new ObservableCollection<ReminderOptionViewModel>
        {
            new(localizationService, "Reminder.AtStart", 0),
            new(localizationService, "Reminder.Before5Minutes", 5),
            new(localizationService, "Reminder.Before10Minutes", 10),
            new(localizationService, "Reminder.Before15Minutes", 15),
            new(localizationService, "Reminder.Before30Minutes", 30),
            new(localizationService, "Reminder.Before1Hour", 60),
            new(localizationService, "Reminder.Before2Hours", 120),
            new(localizationService, "Reminder.Before1Day", 1440)
        };
    }
}
