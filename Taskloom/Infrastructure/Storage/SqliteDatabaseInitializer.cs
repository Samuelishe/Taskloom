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
}
