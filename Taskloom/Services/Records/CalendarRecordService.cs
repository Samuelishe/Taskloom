using Taskloom.Domain;

namespace Taskloom.Services.Records;

/// <summary>
/// Реализует прикладные сценарии создания, редактирования, удаления и фильтрации записей.
/// </summary>
public sealed class CalendarRecordService : ICalendarRecordService
{
    private readonly ICalendarRecordRepository _repository;

    public CalendarRecordService(ICalendarRecordRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CalendarRecord>> GetRecordsByDateAsync(
        DateOnly date,
        RecordType? type = null,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetByDateAsync(date, type, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CalendarRecordDraft?> GetDraftByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _repository.GetByIdAsync(id, cancellationToken);
        return record is null ? null : MapToDraft(record);
    }

    /// <inheritdoc />
    public async Task<CalendarRecord> SaveAsync(CalendarRecordDraft draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var record = CreateRecord(draft);
        await _repository.SaveAsync(record, cancellationToken);

        return record;
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _repository.DeleteAsync(id, cancellationToken);
    }

    private static CalendarRecord CreateRecord(CalendarRecordDraft draft)
    {
        var id = draft.Id ?? Guid.NewGuid();

        return draft.Type switch
        {
            RecordType.Task => new TaskRecord(
                id,
                draft.Date,
                draft.Title,
                draft.Details,
                draft.IsCompleted),

            RecordType.Note => new NoteRecord(
                id,
                draft.Date,
                draft.Title,
                draft.Details),

            RecordType.Event => new EventRecord(
                id,
                draft.Date,
                draft.Title,
                draft.Details,
                GetRequiredTime(draft.StartTime, nameof(draft.StartTime)),
                GetRequiredTime(draft.EndTime, nameof(draft.EndTime)),
                draft.Location),

            RecordType.DaySummary => new DaySummaryRecord(
                id,
                draft.Date,
                draft.Title,
                draft.Details),

            _ => throw new InvalidOperationException($"Неподдерживаемый тип записи: {draft.Type}.")
        };
    }

    private static CalendarRecordDraft MapToDraft(CalendarRecord record)
    {
        var draft = new CalendarRecordDraft
        {
            Id = record.Id,
            Type = record.Type,
            Date = record.Date,
            Title = record.Title,
            Details = record.Details
        };

        switch (record)
        {
            case TaskRecord taskRecord:
                draft.IsCompleted = taskRecord.IsCompleted;
                break;

            case EventRecord eventRecord:
                draft.StartTime = eventRecord.StartTime;
                draft.EndTime = eventRecord.EndTime;
                draft.Location = eventRecord.Location;
                break;
        }

        return draft;
    }

    private static TimeOnly GetRequiredTime(TimeOnly? value, string propertyName)
    {
        return value ?? throw new InvalidOperationException(
            $"Для записи типа Event обязательно значение {propertyName}.");
    }
}
