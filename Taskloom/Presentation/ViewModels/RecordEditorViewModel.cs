using CommunityToolkit.Mvvm.ComponentModel;
using Taskloom.Domain;
using Taskloom.Services.Records;

namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// ViewModel редактора записи.
/// </summary>
public partial class RecordEditorViewModel : ObservableObject
{
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
    private string? location;

    [ObservableProperty]
    private string editorTitle = string.Empty;

    public bool IsTask => Type == RecordType.Task;

    public bool IsEvent => Type == RecordType.Event;

    public bool IsNote => Type == RecordType.Note;

    public bool IsDaySummary => Type == RecordType.DaySummary;

    /// <summary>
    /// Создаёт новый черновик записи для выбранной даты.
    /// </summary>
    public static RecordEditorViewModel CreateNew(DateOnly date)
    {
        return new RecordEditorViewModel
        {
            Type = RecordType.Task,
            Date = date,
            EditorTitle = "Новая запись"
        };
    }

    /// <summary>
    /// Создаёт ViewModel редактора из существующего черновика.
    /// </summary>
    public static RecordEditorViewModel FromDraft(CalendarRecordDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        return new RecordEditorViewModel
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
            EditorTitle = "Редактирование записи"
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
            Location = Location
        };
    }

    partial void OnTypeChanged(RecordType value)
    {
        OnPropertyChanged(nameof(IsTask));
        OnPropertyChanged(nameof(IsEvent));
        OnPropertyChanged(nameof(IsNote));
        OnPropertyChanged(nameof(IsDaySummary));

        if (value != RecordType.Task)
        {
            IsCompleted = false;
        }

        if (value != RecordType.Event)
        {
            StartTime = null;
            EndTime = null;
            Location = null;
        }
    }
}
