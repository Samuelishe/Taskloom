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
    private string? startTimeText;

    [ObservableProperty]
    private string? endTimeText;

    [ObservableProperty]
    private string? location;

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
            StartTimeText = draft.StartTime?.ToString("HH:mm"),
            EndTimeText = draft.EndTime?.ToString("HH:mm"),
            Location = draft.Location,
            EditorTitle = localizationService.GetString("Editor.EditTitle")
        };
    }

    /// <summary>
    /// Возвращает черновик для сохранения.
    /// </summary>
    public CalendarRecordDraft ToDraft()
    {
        var parsedStartTime = TryParseTime(StartTimeText, nameof(StartTimeText));
        var parsedEndTime = TryParseTime(EndTimeText, nameof(EndTimeText));

        return new CalendarRecordDraft
        {
            Id = Id,
            Type = Type,
            Date = Date,
            Title = Title,
            Details = Details,
            IsCompleted = IsCompleted,
            StartTime = parsedStartTime,
            EndTime = parsedEndTime,
            Location = Location
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
            StartTimeText = null;
            EndTimeText = null;
            Location = null;
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
    }

    partial void OnEndTimeChanged(TimeOnly? value)
    {
        ValidationMessage = null;
    }

    partial void OnStartTimeTextChanged(string? value)
    {
        ValidationMessage = null;
    }

    partial void OnEndTimeTextChanged(string? value)
    {
        ValidationMessage = null;
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

    private static TimeOnly? TryParseTime(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (TimeOnly.TryParseExact(value.Trim(), "HH:mm", out var parsedValue))
        {
            return parsedValue;
        }

        throw new InvalidOperationException(propertyName);
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

        TimeOnly? startTime;
        TimeOnly? endTime;

        try
        {
            startTime = TryParseTime(StartTimeText, nameof(StartTimeText));
            endTime = TryParseTime(EndTimeText, nameof(EndTimeText));
        }
        catch (InvalidOperationException exception)
        {
            var labelKey = exception.Message == nameof(StartTimeText)
                ? "Editor.StartTime"
                : "Editor.EndTime";

            return _localizationService.Format(
                "Editor.Validation.TimeFormat",
                _localizationService.GetString(labelKey));
        }

        if (startTime is null || endTime is null)
        {
            return _localizationService.GetString("Editor.Validation.EventTimesRequired");
        }

        if (endTime <= startTime)
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
}
