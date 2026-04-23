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

    /// <summary>
    /// Возвращает путь к файлу настроек приложения.
    /// </summary>
    public static string GetSettingsPath()
    {
        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Taskloom");

        return Path.Combine(appDataDirectory, "settings.json");
    }
}
