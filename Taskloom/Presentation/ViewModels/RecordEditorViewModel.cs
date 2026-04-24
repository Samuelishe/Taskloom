using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Taskloom.Domain;
using Taskloom.Infrastructure.Storage;
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
    private readonly IRecordImageStorageService _imageStorageService;
    private readonly IRecordAudioStorageService _audioStorageService;
    private bool _isTimePartSyncing;

    private RecordEditorViewModel(
        Func<RecordEditorViewModel, CancellationToken, Task> saveAsync,
        Action cancel,
        ILocalizationService localizationService,
        IRecordImageStorageService imageStorageService,
        IRecordAudioStorageService audioStorageService)
    {
        _saveAsync = saveAsync ?? throw new ArgumentNullException(nameof(saveAsync));
        _cancel = cancel ?? throw new ArgumentNullException(nameof(cancel));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _imageStorageService = imageStorageService ?? throw new ArgumentNullException(nameof(imageStorageService));
        _audioStorageService = audioStorageService ?? throw new ArgumentNullException(nameof(audioStorageService));

        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        CancelCommand = new RelayCommand(Cancel);
        RecordTypeOptions = new ObservableCollection<RecordTypeFilterOptionViewModel>
        {
            new(_localizationService, "RecordType.Task", RecordType.Task),
            new(_localizationService, "RecordType.Note", RecordType.Note),
            new(_localizationService, "RecordType.Event", RecordType.Event),
            new(_localizationService, "RecordType.DaySummary", RecordType.DaySummary)
        };
        Images = new ObservableCollection<RecordImageDraft>();
        Audios = new ObservableCollection<RecordAudioDraft>();
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
    private TimeOnly? taskReminderTime;

    [ObservableProperty]
    private bool hasTaskReminder;

    [ObservableProperty]
    private int selectedTaskReminderHour = 9;

    [ObservableProperty]
    private int selectedTaskReminderMinute;

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

    public bool HasImages => Images.Count > 0;

    public bool HasAudios => Audios.Count > 0;

    public DateTime DateValue
    {
        get => Date.ToDateTime(TimeOnly.MinValue);
        set => Date = DateOnly.FromDateTime(value);
    }

    public IAsyncRelayCommand SaveCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public ObservableCollection<RecordTypeFilterOptionViewModel> RecordTypeOptions { get; }

    public ObservableCollection<RecordImageDraft> Images { get; }

    public ObservableCollection<RecordAudioDraft> Audios { get; }

    public IReadOnlyList<TimePartOptionViewModel> HourOptions { get; }

    public IReadOnlyList<TimePartOptionViewModel> MinuteSecondOptions { get; }

    public ObservableCollection<EventStatusOptionViewModel> EventStatusOptions { get; }

    public ObservableCollection<ReminderOptionViewModel> ReminderOptions { get; }

    public event EventHandler<RecordEditorCloseRequestedEventArgs>? CloseRequested;

    public static RecordEditorViewModel CreateNew(
        DateOnly date,
        Func<RecordEditorViewModel, CancellationToken, Task> saveAsync,
        Action cancel,
        ILocalizationService localizationService,
        IRecordImageStorageService imageStorageService,
        IRecordAudioStorageService audioStorageService)
    {
        return new RecordEditorViewModel(saveAsync, cancel, localizationService, imageStorageService, audioStorageService)
        {
            Type = RecordType.Task,
            Date = date,
            EditorTitle = localizationService.GetString("Editor.NewTitle")
        };
    }

    public static RecordEditorViewModel FromDraft(
        CalendarRecordDraft draft,
        Func<RecordEditorViewModel, CancellationToken, Task> saveAsync,
        Action cancel,
        ILocalizationService localizationService,
        IRecordImageStorageService imageStorageService,
        IRecordAudioStorageService audioStorageService)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var viewModel = new RecordEditorViewModel(saveAsync, cancel, localizationService, imageStorageService, audioStorageService)
        {
            Id = draft.Id,
            Type = draft.Type,
            Date = draft.Date,
            Title = draft.Title,
            Details = draft.Details,
            IsCompleted = draft.IsCompleted,
            TaskReminderTime = draft.TaskReminderTime,
            HasTaskReminder = draft.TaskReminderTime is not null,
            StartTime = draft.StartTime,
            EndTime = draft.EndTime,
            Location = draft.Location,
            EventStatus = draft.EventStatus,
            ReminderMinutesBefore = draft.ReminderMinutesBefore,
            EditorTitle = localizationService.GetString("Editor.EditTitle")
        };

        viewModel.LoadImages(draft.Images);
        viewModel.LoadAudios(draft.Audios);
        return viewModel;
    }

    public CalendarRecordDraft ToDraft()
    {
        NormalizeImageSortOrder();
        NormalizeAudioSortOrder();

        return new CalendarRecordDraft
        {
            Id = Id,
            Type = Type,
            Date = Date,
            Title = Title,
            Details = Details,
            Images = Images.Select(CloneImageDraft).ToList(),
            Audios = Audios.Select(CloneAudioDraft).ToList(),
            IsCompleted = IsCompleted,
            TaskReminderTime = TaskReminderTime,
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
            HasTaskReminder = false;
            TaskReminderTime = null;
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

    partial void OnTaskReminderTimeChanged(TimeOnly? value)
    {
        ApplyTaskReminderTimeToParts(value);

        if (_isTimePartSyncing)
        {
            return;
        }

        HasTaskReminder = value is not null;
    }

    partial void OnHasTaskReminderChanged(bool value)
    {
        if (!IsTask)
        {
            return;
        }

        if (value)
        {
            TaskReminderTime ??= new TimeOnly(9, 0);
            return;
        }

        TaskReminderTime = null;
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

    partial void OnSelectedTaskReminderHourChanged(int value)
    {
        UpdateTaskReminderTimeFromParts();
    }

    partial void OnSelectedTaskReminderMinuteChanged(int value)
    {
        UpdateTaskReminderTimeFromParts();
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
            TaskloomDiagnosticLog.AppendException(
                TaskloomPaths.GetRecordSaveLogPath(),
                exception,
                $"Record save failed. RecordId={Id?.ToString() ?? "new"}, Title='{Title}', Type={Type}");
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

    public void AttachImagesFromFiles(IEnumerable<string> filePaths)
    {
        foreach (var filePath in filePaths.Where(static path => !string.IsNullOrWhiteSpace(path)))
        {
            Images.Add(new RecordImageDraft
            {
                OriginalFileName = Path.GetFileName(filePath),
                SourceFilePath = filePath,
                PreviewPath = filePath,
                SortOrder = Images.Count
            });
        }

        NormalizeImageSortOrder();
        OnPropertyChanged(nameof(HasImages));
    }

    public async Task AttachAudiosFromFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
    {
        foreach (var filePath in filePaths.Where(static path => !string.IsNullOrWhiteSpace(path)))
        {
            var draft = await _audioStorageService.PrepareAudioDraftAsync(filePath, Audios.Count, cancellationToken);
            Audios.Add(draft);
        }

        NormalizeAudioSortOrder();
        OnPropertyChanged(nameof(HasAudios));
    }

    public void RemoveImage(RecordImageDraft? image)
    {
        if (image is null)
        {
            return;
        }

        Images.Remove(image);
        NormalizeImageSortOrder();
        OnPropertyChanged(nameof(HasImages));
    }

    public void RemoveAudio(RecordAudioDraft? audio)
    {
        if (audio is null)
        {
            return;
        }

        Audios.Remove(audio);
        NormalizeAudioSortOrder();
        OnPropertyChanged(nameof(HasAudios));
    }

    public void MoveImageLeft(RecordImageDraft? image)
    {
        MoveImage(image, -1);
    }

    public void MoveImageRight(RecordImageDraft? image)
    {
        MoveImage(image, 1);
    }

    public void MoveAudioLeft(RecordAudioDraft? audio)
    {
        MoveAudio(audio, -1);
    }

    public void MoveAudioRight(RecordAudioDraft? audio)
    {
        MoveAudio(audio, 1);
    }

    public bool CanMoveImageLeft(RecordImageDraft? image)
    {
        return image is not null && Images.IndexOf(image) > 0;
    }

    public bool CanMoveImageRight(RecordImageDraft? image)
    {
        return image is not null && Images.IndexOf(image) >= 0 && Images.IndexOf(image) < Images.Count - 1;
    }

    public bool CanMoveAudioLeft(RecordAudioDraft? audio)
    {
        return audio is not null && Audios.IndexOf(audio) > 0;
    }

    public bool CanMoveAudioRight(RecordAudioDraft? audio)
    {
        return audio is not null && Audios.IndexOf(audio) >= 0 && Audios.IndexOf(audio) < Audios.Count - 1;
    }

    private void MoveImage(RecordImageDraft? image, int delta)
    {
        if (image is null)
        {
            return;
        }

        var oldIndex = Images.IndexOf(image);

        if (oldIndex < 0)
        {
            return;
        }

        var newIndex = oldIndex + delta;

        if (newIndex < 0 || newIndex >= Images.Count)
        {
            return;
        }

        Images.Move(oldIndex, newIndex);
        NormalizeImageSortOrder();
    }

    private void MoveAudio(RecordAudioDraft? audio, int delta)
    {
        if (audio is null)
        {
            return;
        }

        var oldIndex = Audios.IndexOf(audio);

        if (oldIndex < 0)
        {
            return;
        }

        var newIndex = oldIndex + delta;

        if (newIndex < 0 || newIndex >= Audios.Count)
        {
            return;
        }

        Audios.Move(oldIndex, newIndex);
        NormalizeAudioSortOrder();
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

    private void ApplyTaskReminderTimeToParts(TimeOnly? value)
    {
        if (value is null || _isTimePartSyncing)
        {
            return;
        }

        _isTimePartSyncing = true;

        try
        {
            SelectedTaskReminderHour = value.Value.Hour;
            SelectedTaskReminderMinute = value.Value.Minute;
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

    private void UpdateTaskReminderTimeFromParts()
    {
        if (_isTimePartSyncing || !IsTask || !HasTaskReminder)
        {
            return;
        }

        TaskReminderTime = new TimeOnly(SelectedTaskReminderHour, SelectedTaskReminderMinute);
    }

    private void UpdateEndTimeFromParts()
    {
        if (_isTimePartSyncing || !IsEvent)
        {
            return;
        }

        EndTime = new TimeOnly(SelectedEndHour, SelectedEndMinute, SelectedEndSecond);
    }

    private void LoadImages(IEnumerable<RecordImageDraft> images)
    {
        Images.Clear();

        foreach (var image in images.OrderBy(static image => image.SortOrder))
        {
            Images.Add(new RecordImageDraft
            {
                Id = image.Id,
                OriginalFileName = image.OriginalFileName,
                StoredFileName = image.StoredFileName,
                RelativePath = image.RelativePath,
                ContentType = image.ContentType,
                FileSize = image.FileSize,
                CreatedUtc = image.CreatedUtc,
                SortOrder = image.SortOrder,
                SourceFilePath = image.SourceFilePath,
                PreviewPath = image.IsPendingImport
                    ? image.SourceFilePath
                    : _imageStorageService.GetAbsolutePath(image.RelativePath)
            });
        }

        NormalizeImageSortOrder();
        OnPropertyChanged(nameof(HasImages));
    }

    private void LoadAudios(IEnumerable<RecordAudioDraft> audios)
    {
        Audios.Clear();

        foreach (var audio in audios.OrderBy(static audio => audio.SortOrder))
        {
            Audios.Add(new RecordAudioDraft
            {
                Id = audio.Id,
                OriginalFileName = audio.OriginalFileName,
                StoredFileName = audio.StoredFileName,
                RelativePath = audio.RelativePath,
                ContentType = audio.ContentType,
                FileSize = audio.FileSize,
                CreatedUtc = audio.CreatedUtc,
                SortOrder = audio.SortOrder,
                SourceFilePath = audio.SourceFilePath,
                DisplayTitle = audio.DisplayTitle,
                DurationSeconds = audio.DurationSeconds,
                AlbumTitle = audio.AlbumTitle,
                Genre = audio.Genre,
                CoverRelativePath = audio.CoverRelativePath,
                CoverPreviewPath = audio.IsPendingImport
                    ? audio.CoverPreviewPath
                    : _audioStorageService.GetAbsolutePath(audio.CoverRelativePath),
                CoverBytes = audio.CoverBytes
            });
        }

        NormalizeAudioSortOrder();
        OnPropertyChanged(nameof(HasAudios));
    }

    private void NormalizeImageSortOrder()
    {
        for (var index = 0; index < Images.Count; index++)
        {
            Images[index].SortOrder = index;

            if (Images[index].PreviewPath is null)
            {
                Images[index].PreviewPath = Images[index].IsPendingImport
                    ? Images[index].SourceFilePath
                    : _imageStorageService.GetAbsolutePath(Images[index].RelativePath);
            }
        }
    }

    private void NormalizeAudioSortOrder()
    {
        for (var index = 0; index < Audios.Count; index++)
        {
            Audios[index].SortOrder = index;

            if (Audios[index].CoverPreviewPath is null && !string.IsNullOrWhiteSpace(Audios[index].CoverRelativePath))
            {
                Audios[index].CoverPreviewPath = Audios[index].IsPendingImport
                    ? Audios[index].CoverPreviewPath
                    : _audioStorageService.GetAbsolutePath(Audios[index].CoverRelativePath);
            }
        }
    }

    private static RecordImageDraft CloneImageDraft(RecordImageDraft image)
    {
        return new RecordImageDraft
        {
            Id = image.Id,
            OriginalFileName = image.OriginalFileName,
            StoredFileName = image.StoredFileName,
            RelativePath = image.RelativePath,
            ContentType = image.ContentType,
            FileSize = image.FileSize,
            CreatedUtc = image.CreatedUtc,
            SortOrder = image.SortOrder,
            SourceFilePath = image.SourceFilePath,
            PreviewPath = image.PreviewPath
        };
    }

    private static RecordAudioDraft CloneAudioDraft(RecordAudioDraft audio)
    {
        return new RecordAudioDraft
        {
            Id = audio.Id,
            OriginalFileName = audio.OriginalFileName,
            StoredFileName = audio.StoredFileName,
            RelativePath = audio.RelativePath,
            ContentType = audio.ContentType,
            FileSize = audio.FileSize,
            CreatedUtc = audio.CreatedUtc,
            SortOrder = audio.SortOrder,
            SourceFilePath = audio.SourceFilePath,
            DisplayTitle = audio.DisplayTitle,
            DurationSeconds = audio.DurationSeconds,
            AlbumTitle = audio.AlbumTitle,
            Genre = audio.Genre,
            CoverRelativePath = audio.CoverRelativePath,
            CoverPreviewPath = audio.CoverPreviewPath,
            CoverBytes = audio.CoverBytes
        };
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
        return
        [
            new EventStatusOptionViewModel(localizationService, "EventStatus.Scheduled", EventStatus.Scheduled),
            new EventStatusOptionViewModel(localizationService, "EventStatus.Completed", EventStatus.Completed),
            new EventStatusOptionViewModel(localizationService, "EventStatus.Rescheduled", EventStatus.Rescheduled),
            new EventStatusOptionViewModel(localizationService, "EventStatus.Canceled", EventStatus.Canceled)
        ];
    }

    private static ObservableCollection<ReminderOptionViewModel> CreateReminderOptions(ILocalizationService localizationService)
    {
        return
        [
            new ReminderOptionViewModel(localizationService, "Reminder.AtStart", 0),
            new ReminderOptionViewModel(localizationService, "Reminder.Before5Minutes", 5),
            new ReminderOptionViewModel(localizationService, "Reminder.Before10Minutes", 10),
            new ReminderOptionViewModel(localizationService, "Reminder.Before15Minutes", 15),
            new ReminderOptionViewModel(localizationService, "Reminder.Before30Minutes", 30),
            new ReminderOptionViewModel(localizationService, "Reminder.Before1Hour", 60),
            new ReminderOptionViewModel(localizationService, "Reminder.Before2Hours", 120),
            new ReminderOptionViewModel(localizationService, "Reminder.Before1Day", 1440)
        ];
    }
}
