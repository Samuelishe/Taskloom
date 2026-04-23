using System.IO;

namespace Taskloom.Infrastructure.Storage;

/// <summary>
/// Вычисляет стандартные пути хранения файлов приложения.
/// </summary>
public static class TaskloomPaths
{
    /// <summary>
    /// Возвращает путь к SQLite-файлу приложения в локальном профиле пользователя.
    /// </summary>
    public static string GetDatabasePath()
    {
        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Taskloom");

        return Path.Combine(appDataDirectory, "taskloom.db");
    }
}
