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
    }
}
