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
                               sort_order AS SortOrder,
                               hide_links_when_preview_available AS HideLinksWhenPreviewAvailable,
                               created_utc AS CreatedUtc,
                               is_completed AS IsCompleted,
                               task_reminder_time AS TaskReminderTime,
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
        if (dataModel is null)
        {
            return null;
        }

        var record = MapToDomain(dataModel);
        var attachmentsByRecordId = await LoadAttachmentsAsync(connection, [record.Id], cancellationToken);
        AttachAttachments([record], attachmentsByRecordId);
        return record;
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
                               sort_order AS SortOrder,
                               hide_links_when_preview_available AS HideLinksWhenPreviewAvailable,
                               created_utc AS CreatedUtc,
                               is_completed AS IsCompleted,
                               task_reminder_time AS TaskReminderTime,
                               start_time AS StartTime,
                               end_time AS EndTime,
                               location AS Location,
                               event_status_id AS EventStatusId,
                               reminder_minutes_before AS ReminderMinutesBefore
                           FROM calendar_records
                           WHERE record_date = @Date
                             AND (@TypeId IS NULL OR type_id = @TypeId)
                           ORDER BY
                               sort_order,
                               CASE WHEN start_time IS NULL THEN 1 ELSE 0 END,
                               start_time,
                               title,
                               created_utc,
                               id;
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

        var dataModels = (await connection.QueryAsync<CalendarRecordDataModel>(command)).ToArray();
        var records = dataModels.Select(MapToDomain).ToArray();
        var attachmentsByRecordId = await LoadAttachmentsAsync(connection, records.Select(record => record.Id), cancellationToken);
        AttachAttachments(records, attachmentsByRecordId);
        return records;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CalendarRecord>> GetOlderThanAsync(
        DateOnly cutoffDateExclusive,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT
                               id AS Id,
                               type_id AS TypeId,
                               record_date AS Date,
                               title AS Title,
                               details AS Details,
                               sort_order AS SortOrder,
                               hide_links_when_preview_available AS HideLinksWhenPreviewAvailable,
                               created_utc AS CreatedUtc,
                               is_completed AS IsCompleted,
                               task_reminder_time AS TaskReminderTime,
                               start_time AS StartTime,
                               end_time AS EndTime,
                               location AS Location,
                               event_status_id AS EventStatusId,
                               reminder_minutes_before AS ReminderMinutesBefore
                           FROM calendar_records
                           WHERE record_date < @CutoffDate
                           ORDER BY record_date, sort_order, created_utc, title;
                           """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new { CutoffDate = cutoffDateExclusive.ToString("yyyy-MM-dd") },
            cancellationToken: cancellationToken);

        var dataModels = (await connection.QueryAsync<CalendarRecordDataModel>(command)).ToArray();
        var records = dataModels.Select(MapToDomain).ToArray();
        var attachmentsByRecordId = await LoadAttachmentsAsync(connection, records.Select(record => record.Id), cancellationToken);
        AttachAttachments(records, attachmentsByRecordId);
        return records;
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
                               sort_order,
                               hide_links_when_preview_available,
                               created_utc,
                               is_completed,
                               task_reminder_time,
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
                               @SortOrder,
                               @HideLinksWhenPreviewAvailable,
                               @CreatedUtc,
                               @IsCompleted,
                               @TaskReminderTime,
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
                               sort_order = excluded.sort_order,
                               hide_links_when_preview_available = excluded.hide_links_when_preview_available,
                               created_utc = excluded.created_utc,
                               is_completed = excluded.is_completed,
                               task_reminder_time = excluded.task_reminder_time,
                               start_time = excluded.start_time,
                               end_time = excluded.end_time,
                               location = excluded.location,
                               event_status_id = excluded.event_status_id,
                               reminder_minutes_before = excluded.reminder_minutes_before;
                           """;

        var dataModel = MapToDataModel(record);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var command = new CommandDefinition(sql, dataModel, transaction, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);

        const string deleteAttachmentsSql = """
                                            DELETE FROM record_attachments
                                            WHERE record_id = @RecordId;
                                            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                deleteAttachmentsSql,
                new { RecordId = record.Id.ToString("D") },
                transaction,
                cancellationToken: cancellationToken));

        if (record.Attachments.Count > 0)
        {
            const string insertAttachmentSql = """
                                               INSERT INTO record_attachments
                                               (
                                                   id,
                                                   record_id,
                                                   kind_id,
                                                   original_file_name,
                                                   stored_file_name,
                                                   relative_path,
                                                   content_type,
                                                   file_size,
                                                   created_utc,
                                                   sort_order,
                                                   display_title,
                                                   duration_seconds,
                                                   preview_relative_path,
                                                   album_title,
                                                   genre
                                               )
                                               VALUES
                                               (
                                                   @Id,
                                                   @RecordId,
                                                   @KindId,
                                                   @OriginalFileName,
                                                   @StoredFileName,
                                                   @RelativePath,
                                                   @ContentType,
                                                   @FileSize,
                                                   @CreatedUtc,
                                                   @SortOrder,
                                                   @DisplayTitle,
                                                   @DurationSeconds,
                                                   @PreviewRelativePath,
                                                   @AlbumTitle,
                                                   @Genre
                                               );
                                               """;

            var attachmentDataModels = record.Attachments
                .Select(MapAttachmentToDataModel)
                .ToArray();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertAttachmentSql,
                    attachmentDataModels,
                    transaction,
                    cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateSortOrdersAsync(DateOnly date, IReadOnlyList<Guid> orderedRecordIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orderedRecordIds);

        if (orderedRecordIds.Count == 0)
        {
            return;
        }

        const string readSql = """
                               SELECT id
                               FROM calendar_records
                               WHERE record_date = @Date;
                               """;

        const string updateSql = """
                                 UPDATE calendar_records
                                 SET sort_order = @SortOrder
                                 WHERE id = @Id
                                   AND record_date = @Date;
                                 """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var existingIds = (await connection.QueryAsync<string>(
            new CommandDefinition(
                readSql,
                new { Date = date.ToString("yyyy-MM-dd") },
                cancellationToken: cancellationToken)))
            .Select(Guid.Parse)
            .ToArray();

        if (existingIds.Length != orderedRecordIds.Count ||
            existingIds.Except(orderedRecordIds).Any() ||
            orderedRecordIds.Except(existingIds).Any())
        {
            throw new InvalidOperationException("Нельзя сохранить порядок: состав записей выбранной даты изменился.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        for (var index = 0; index < orderedRecordIds.Count; index++)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    updateSql,
                    new
                    {
                        Id = orderedRecordIds[index].ToString("D"),
                        Date = date.ToString("yyyy-MM-dd"),
                        SortOrder = index
                    },
                    transaction,
                    cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string deleteAttachmentsSql = """
                                            DELETE FROM record_attachments
                                            WHERE record_id = @Id;
                                            """;

        const string deleteRecordSql = """
                                       DELETE FROM calendar_records
                                       WHERE id = @Id;
                                       """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var parameters = new { Id = id.ToString("D") };
        await connection.ExecuteAsync(new CommandDefinition(deleteAttachmentsSql, parameters, transaction, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition(deleteRecordSql, parameters, transaction, cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
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
                dataModel.IsCompleted ?? false,
                ParseOptionalTime(dataModel.TaskReminderTime),
                dataModel.SortOrder,
                dataModel.HideLinksWhenPreviewAvailable,
                ParseRequiredUtc(dataModel.CreatedUtc)),

            RecordType.Note => new NoteRecord(
                id,
                date,
                dataModel.Title,
                dataModel.Details,
                dataModel.SortOrder,
                dataModel.HideLinksWhenPreviewAvailable,
                ParseRequiredUtc(dataModel.CreatedUtc)),

            RecordType.Event => new EventRecord(
                id,
                date,
                dataModel.Title,
                dataModel.Details,
                ParseRequiredTime(dataModel.StartTime, nameof(dataModel.StartTime)),
                ParseRequiredTime(dataModel.EndTime, nameof(dataModel.EndTime)),
                dataModel.Location,
                (EventStatus)(dataModel.EventStatusId ?? (int)EventStatus.Scheduled),
                dataModel.ReminderMinutesBefore ?? 60,
                dataModel.SortOrder,
                dataModel.HideLinksWhenPreviewAvailable,
                ParseRequiredUtc(dataModel.CreatedUtc)),

            RecordType.DaySummary => new DaySummaryRecord(
                id,
                date,
                dataModel.Title,
                dataModel.Details,
                dataModel.SortOrder,
                dataModel.HideLinksWhenPreviewAvailable,
                ParseRequiredUtc(dataModel.CreatedUtc)),

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
            Details = record.Details,
            SortOrder = record.SortOrder,
            HideLinksWhenPreviewAvailable = record.HideLinksWhenPreviewAvailable,
            CreatedUtc = record.CreatedUtc.ToString("O")
        };

        switch (record)
        {
            case TaskRecord taskRecord:
                dataModel.IsCompleted = taskRecord.IsCompleted;
                dataModel.TaskReminderTime = taskRecord.ReminderTime?.ToString("HH:mm");
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

    private static RecordAttachmentDataModel MapAttachmentToDataModel(RecordAttachment attachment)
    {
        return new RecordAttachmentDataModel
        {
            Id = attachment.Id.ToString("D"),
            RecordId = attachment.RecordId.ToString("D"),
            KindId = (int)attachment.Kind,
            OriginalFileName = attachment.OriginalFileName,
            StoredFileName = attachment.StoredFileName,
            RelativePath = attachment.RelativePath,
            ContentType = attachment.ContentType,
            FileSize = attachment.FileSize,
            CreatedUtc = attachment.CreatedUtc.ToString("O"),
            SortOrder = attachment.SortOrder,
            DisplayTitle = attachment.DisplayTitle,
            DurationSeconds = attachment.DurationSeconds,
            PreviewRelativePath = attachment.PreviewRelativePath,
            AlbumTitle = attachment.AlbumTitle,
            Genre = attachment.Genre
        };
    }

    private static RecordAttachment MapAttachmentToDomain(RecordAttachmentDataModel dataModel)
    {
        return new RecordAttachment(
            Guid.Parse(dataModel.Id),
            Guid.Parse(dataModel.RecordId),
            (RecordAttachmentKind)dataModel.KindId,
            dataModel.OriginalFileName,
            dataModel.StoredFileName,
            dataModel.RelativePath,
            dataModel.ContentType,
            dataModel.FileSize,
            DateTime.Parse(dataModel.CreatedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind),
            dataModel.SortOrder,
            dataModel.DisplayTitle,
            dataModel.DurationSeconds,
            dataModel.PreviewRelativePath,
            dataModel.AlbumTitle,
            dataModel.Genre);
    }

    private static void AttachAttachments(
        IEnumerable<CalendarRecord> records,
        IReadOnlyDictionary<Guid, IReadOnlyList<RecordAttachment>> attachmentsByRecordId)
    {
        foreach (var record in records)
        {
            record.ReplaceAttachments(
                attachmentsByRecordId.TryGetValue(record.Id, out var attachments)
                    ? attachments
                    : []);
        }
    }

    private static async Task<IReadOnlyDictionary<Guid, IReadOnlyList<RecordAttachment>>> LoadAttachmentsAsync(
        System.Data.IDbConnection connection,
        IEnumerable<Guid> recordIds,
        CancellationToken cancellationToken)
    {
        var ids = recordIds
            .Distinct()
            .Select(id => id.ToString("D"))
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<RecordAttachment>>();
        }

        const string sql = """
                           SELECT
                               id AS Id,
                               record_id AS RecordId,
                               kind_id AS KindId,
                               original_file_name AS OriginalFileName,
                               stored_file_name AS StoredFileName,
                               relative_path AS RelativePath,
                               content_type AS ContentType,
                               file_size AS FileSize,
                               created_utc AS CreatedUtc,
                               sort_order AS SortOrder,
                               display_title AS DisplayTitle,
                               duration_seconds AS DurationSeconds,
                               preview_relative_path AS PreviewRelativePath,
                               album_title AS AlbumTitle,
                               genre AS Genre
                           FROM record_attachments
                           WHERE record_id IN @RecordIds
                           ORDER BY sort_order, created_utc;
                           """;

        var command = new CommandDefinition(sql, new { RecordIds = ids }, cancellationToken: cancellationToken);
        var dataModels = await connection.QueryAsync<RecordAttachmentDataModel>(command);

        return dataModels
            .Select(MapAttachmentToDomain)
            .GroupBy(attachment => attachment.RecordId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<RecordAttachment>)group.ToArray());
    }

    private static TimeOnly ParseRequiredTime(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Для события отсутствует обязательное значение {propertyName}.");
        }

        return TimeOnly.ParseExact(value, "HH:mm");
    }

    private static TimeOnly? ParseOptionalTime(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : TimeOnly.ParseExact(value, "HH:mm");
    }

    private static DateTime ParseRequiredUtc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DateTime.UtcNow;
        }

        return DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
    }
}
