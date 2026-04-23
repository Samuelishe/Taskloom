using Dapper;
using Taskloom.Data;
using Taskloom.Data.Models;
using Taskloom.Domain;
using Taskloom.Infrastructure.Storage;
using Taskloom.Services.Records;

namespace Taskloom.Infrastructure.Repositories;

/// <summary>
/// SQLite-реализация репозитория календарных записей.
/// </summary>
public sealed class SqliteCalendarRecordRepository : ICalendarRecordRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteCalendarRecordRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <inheritdoc />
    public async Task<CalendarRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT
                               id AS Id,
                               type_id AS TypeId,
                               record_date AS Date,
                               title AS Title,
                               details AS Details,
                               is_completed AS IsCompleted,
                               start_time AS StartTime,
                               end_time AS EndTime,
                               location AS Location,
                               event_status_id AS EventStatusId,
                               reminder_minutes_before AS ReminderMinutesBefore
                           FROM calendar_records
                           WHERE id = @Id;
                           """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Id = id.ToString("D") }, cancellationToken: cancellationToken);
        var dataModel = await connection.QuerySingleOrDefaultAsync<CalendarRecordDataModel>(command);

        return dataModel is null ? null : MapToDomain(dataModel);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CalendarRecord>> GetByDateAsync(
        DateOnly date,
        RecordType? type = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT
                               id AS Id,
                               type_id AS TypeId,
                               record_date AS Date,
                               title AS Title,
                               details AS Details,
                               is_completed AS IsCompleted,
                               start_time AS StartTime,
                               end_time AS EndTime,
                               location AS Location,
                               event_status_id AS EventStatusId,
                               reminder_minutes_before AS ReminderMinutesBefore
                           FROM calendar_records
                           WHERE record_date = @Date
                             AND (@TypeId IS NULL OR type_id = @TypeId)
                           ORDER BY
                               CASE WHEN start_time IS NULL THEN 1 ELSE 0 END,
                               start_time,
                               title;
                           """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                Date = date.ToString("yyyy-MM-dd"),
                TypeId = type is null ? (int?)null : (int)type.Value
            },
            cancellationToken: cancellationToken);

        var dataModels = await connection.QueryAsync<CalendarRecordDataModel>(command);
        return dataModels.Select(MapToDomain).ToArray();
    }

    /// <inheritdoc />
    public async Task SaveAsync(CalendarRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        const string sql = """
                           INSERT INTO calendar_records
                           (
                               id,
                               type_id,
                               record_date,
                               title,
                               details,
                               is_completed,
                               start_time,
                               end_time,
                               location,
                               event_status_id,
                               reminder_minutes_before
                           )
                           VALUES
                           (
                               @Id,
                               @TypeId,
                               @Date,
                               @Title,
                               @Details,
                               @IsCompleted,
                               @StartTime,
                               @EndTime,
                               @Location,
                               @EventStatusId,
                               @ReminderMinutesBefore
                           )
                           ON CONFLICT(id) DO UPDATE SET
                               type_id = excluded.type_id,
                               record_date = excluded.record_date,
                               title = excluded.title,
                               details = excluded.details,
                               is_completed = excluded.is_completed,
                               start_time = excluded.start_time,
                               end_time = excluded.end_time,
                               location = excluded.location,
                               event_status_id = excluded.event_status_id,
                               reminder_minutes_before = excluded.reminder_minutes_before;
                           """;

        var dataModel = MapToDataModel(record);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, dataModel, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           DELETE FROM calendar_records
                           WHERE id = @Id;
                           """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Id = id.ToString("D") }, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    private static CalendarRecord MapToDomain(CalendarRecordDataModel dataModel)
    {
        var recordType = (RecordType)dataModel.TypeId;
        var id = Guid.Parse(dataModel.Id);
        var date = DateOnly.ParseExact(dataModel.Date, "yyyy-MM-dd");

        return recordType switch
        {
            RecordType.Task => new TaskRecord(
                id,
                date,
                dataModel.Title,
                dataModel.Details,
                dataModel.IsCompleted ?? false),

            RecordType.Note => new NoteRecord(
                id,
                date,
                dataModel.Title,
                dataModel.Details),

            RecordType.Event => new EventRecord(
                id,
                date,
                dataModel.Title,
                dataModel.Details,
                ParseRequiredTime(dataModel.StartTime, nameof(dataModel.StartTime)),
                ParseRequiredTime(dataModel.EndTime, nameof(dataModel.EndTime)),
                dataModel.Location,
                (EventStatus)(dataModel.EventStatusId ?? (int)EventStatus.Scheduled),
                dataModel.ReminderMinutesBefore ?? 60),

            RecordType.DaySummary => new DaySummaryRecord(
                id,
                date,
                dataModel.Title,
                dataModel.Details),

            _ => throw new InvalidOperationException($"Неподдерживаемый тип записи: {dataModel.TypeId}.")
        };
    }

    private static CalendarRecordDataModel MapToDataModel(CalendarRecord record)
    {
        var dataModel = new CalendarRecordDataModel
        {
            Id = record.Id.ToString("D"),
            TypeId = (int)record.Type,
            Date = record.Date.ToString("yyyy-MM-dd"),
            Title = record.Title,
            Details = record.Details
        };

        switch (record)
        {
            case TaskRecord taskRecord:
                dataModel.IsCompleted = taskRecord.IsCompleted;
                break;

            case EventRecord eventRecord:
                dataModel.StartTime = eventRecord.StartTime.ToString("HH:mm");
                dataModel.EndTime = eventRecord.EndTime.ToString("HH:mm");
                dataModel.Location = eventRecord.Location;
                dataModel.EventStatusId = (int)eventRecord.Status;
                dataModel.ReminderMinutesBefore = eventRecord.ReminderMinutesBefore;
                break;
        }

        return dataModel;
    }

    private static TimeOnly ParseRequiredTime(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Для события отсутствует обязательное значение {propertyName}.");
        }

        return TimeOnly.ParseExact(value, "HH:mm");
    }
}
