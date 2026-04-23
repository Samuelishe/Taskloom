using System.IO;
using Dapper;
using Taskloom.Data;

namespace Taskloom.Infrastructure.Storage;

/// <summary>
/// Инициализирует SQLite schema для приложения.
/// </summary>
public sealed class SqliteDatabaseInitializer
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteDatabaseInitializer(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <summary>
    /// Создаёт схему базы данных, если она ещё не существует.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var scriptPath = Path.Combine(AppContext.BaseDirectory, DatabaseSchema.CreateSchemaScriptPath);

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException("Не найден SQL-скрипт инициализации базы данных.", scriptPath);
        }

        var sql = await File.ReadAllTextAsync(scriptPath, cancellationToken);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
        await EnsureColumnAsync(connection, "event_status_id", "INTEGER NULL", cancellationToken);
        await EnsureColumnAsync(connection, "reminder_minutes_before", "INTEGER NULL", cancellationToken);
        await EnsureColumnAsync(connection, "task_reminder_time", "TEXT NULL", cancellationToken);
        await EnsureRecordAttachmentsTableAsync(connection, cancellationToken);
        await EnsureRecordAttachmentsSchemaAsync(connection, cancellationToken);
        await RecreateRecordAttachmentIndexesAsync(connection, cancellationToken);
    }

    private static async Task EnsureColumnAsync(
        System.Data.IDbConnection connection,
        string columnName,
        string columnDefinition,
        CancellationToken cancellationToken)
    {
        const string columnsSql = "PRAGMA table_info(calendar_records);";
        var columns = await connection.QueryAsync(
            new CommandDefinition(
                columnsSql,
                cancellationToken: cancellationToken));

        if (columns.Any(column =>
        {
            var row = (IDictionary<string, object>)column;
            return string.Equals(row["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase);
        }))
        {
            return;
        }

        var alterSql = $"ALTER TABLE calendar_records ADD COLUMN {columnName} {columnDefinition};";
        await connection.ExecuteAsync(new CommandDefinition(alterSql, cancellationToken: cancellationToken));
    }

    private static async Task EnsureRecordAttachmentColumnAsync(
        System.Data.IDbConnection connection,
        string columnName,
        string columnDefinition,
        CancellationToken cancellationToken)
    {
        const string columnsSql = "PRAGMA table_info(record_attachments);";
        var columns = await connection.QueryAsync(
            new CommandDefinition(
                columnsSql,
                cancellationToken: cancellationToken));

        if (columns.Any(column =>
        {
            var row = (IDictionary<string, object>)column;
            return string.Equals(row["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase);
        }))
        {
            return;
        }

        var alterSql = $"ALTER TABLE record_attachments ADD COLUMN {columnName} {columnDefinition};";
        await connection.ExecuteAsync(new CommandDefinition(alterSql, cancellationToken: cancellationToken));
    }

    private static async Task EnsureRecordAttachmentsTableAsync(
        System.Data.IDbConnection connection,
        CancellationToken cancellationToken)
    {
        const string createSql = """
                                 CREATE TABLE IF NOT EXISTS record_attachments
                                 (
                                     id TEXT NOT NULL PRIMARY KEY,
                                     record_id TEXT NOT NULL,
                                     kind_id INTEGER NOT NULL,
                                     original_file_name TEXT NOT NULL,
                                     stored_file_name TEXT NOT NULL,
                                     relative_path TEXT NOT NULL,
                                     content_type TEXT NULL,
                                     file_size INTEGER NOT NULL,
                                     created_utc TEXT NOT NULL,
                                     sort_order INTEGER NOT NULL DEFAULT 0,
                                     display_title TEXT NULL,
                                     duration_seconds REAL NULL,
                                     preview_relative_path TEXT NULL,
                                     CHECK (length(trim(original_file_name)) > 0),
                                     CHECK (length(original_file_name) <= 260),
                                     CHECK (length(trim(stored_file_name)) > 0),
                                     CHECK (length(stored_file_name) <= 260),
                                     CHECK (length(trim(relative_path)) > 0),
                                     CHECK (length(relative_path) <= 512),
                                     CHECK (content_type IS NULL OR length(content_type) <= 100),
                                     CHECK (display_title IS NULL OR length(display_title) <= 260),
                                     CHECK (preview_relative_path IS NULL OR length(preview_relative_path) <= 512),
                                     CHECK (file_size >= 0),
                                     CHECK (sort_order >= 0),
                                     CHECK (duration_seconds IS NULL OR duration_seconds >= 0)
                                 );

                                 CREATE INDEX IF NOT EXISTS ix_record_attachments_record_id
                                     ON record_attachments(record_id);
                                 """;

        await connection.ExecuteAsync(new CommandDefinition(createSql, cancellationToken: cancellationToken));
    }

    private static async Task EnsureRecordAttachmentsSchemaAsync(
        System.Data.IDbConnection connection,
        CancellationToken cancellationToken)
    {
        const string tableSql = """
                                SELECT sql
                                FROM sqlite_master
                                WHERE type = 'table'
                                  AND name = 'record_attachments';
                                """;

        var tableDefinition = await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(tableSql, cancellationToken: cancellationToken));

        if (string.IsNullOrWhiteSpace(tableDefinition))
        {
            return;
        }

        var normalizedSql = tableDefinition.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(Environment.NewLine, string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        var requiresRebuild =
            normalizedSql.Contains("check(kind_idin(1))", StringComparison.Ordinal) ||
            !normalizedSql.Contains("display_title", StringComparison.Ordinal) ||
            !normalizedSql.Contains("duration_seconds", StringComparison.Ordinal) ||
            !normalizedSql.Contains("preview_relative_path", StringComparison.Ordinal);

        if (requiresRebuild)
        {
            const string rebuildSql = """
                                      DROP INDEX IF EXISTS ix_record_attachments_record_id;
                                      DROP INDEX IF EXISTS ix_record_attachments_record_sort;
                                      DROP INDEX IF EXISTS ux_record_attachments_record_image;

                                      CREATE TABLE record_attachments_new
                                      (
                                          id TEXT NOT NULL PRIMARY KEY,
                                          record_id TEXT NOT NULL,
                                          kind_id INTEGER NOT NULL,
                                          original_file_name TEXT NOT NULL,
                                          stored_file_name TEXT NOT NULL,
                                          relative_path TEXT NOT NULL,
                                          content_type TEXT NULL,
                                          file_size INTEGER NOT NULL,
                                          created_utc TEXT NOT NULL,
                                          sort_order INTEGER NOT NULL DEFAULT 0,
                                          display_title TEXT NULL,
                                          duration_seconds REAL NULL,
                                          preview_relative_path TEXT NULL,
                                          CHECK (length(trim(original_file_name)) > 0),
                                          CHECK (length(original_file_name) <= 260),
                                          CHECK (length(trim(stored_file_name)) > 0),
                                          CHECK (length(stored_file_name) <= 260),
                                          CHECK (length(trim(relative_path)) > 0),
                                          CHECK (length(relative_path) <= 512),
                                          CHECK (content_type IS NULL OR length(content_type) <= 100),
                                          CHECK (display_title IS NULL OR length(display_title) <= 260),
                                          CHECK (preview_relative_path IS NULL OR length(preview_relative_path) <= 512),
                                          CHECK (file_size >= 0),
                                          CHECK (sort_order >= 0),
                                          CHECK (duration_seconds IS NULL OR duration_seconds >= 0)
                                      );

                                      INSERT INTO record_attachments_new
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
                                          preview_relative_path
                                      )
                                      SELECT
                                          id,
                                          record_id,
                                          kind_id,
                                          original_file_name,
                                          stored_file_name,
                                          relative_path,
                                          content_type,
                                          file_size,
                                          created_utc,
                                          COALESCE(sort_order, 0),
                                          NULL,
                                          NULL,
                                          NULL
                                      FROM record_attachments;

                                      DROP TABLE record_attachments;
                                      ALTER TABLE record_attachments_new RENAME TO record_attachments;
                                      """;

            await connection.ExecuteAsync(new CommandDefinition(rebuildSql, cancellationToken: cancellationToken));
            return;
        }

        await EnsureRecordAttachmentColumnAsync(connection, "sort_order", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await EnsureRecordAttachmentColumnAsync(connection, "display_title", "TEXT NULL", cancellationToken);
        await EnsureRecordAttachmentColumnAsync(connection, "duration_seconds", "REAL NULL", cancellationToken);
        await EnsureRecordAttachmentColumnAsync(connection, "preview_relative_path", "TEXT NULL", cancellationToken);
    }

    private static async Task RecreateRecordAttachmentIndexesAsync(
        System.Data.IDbConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           DROP INDEX IF EXISTS ux_record_attachments_record_image;

                           CREATE INDEX IF NOT EXISTS ix_record_attachments_record_sort
                               ON record_attachments(record_id, sort_order, created_utc);
                           """;

        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }
}
