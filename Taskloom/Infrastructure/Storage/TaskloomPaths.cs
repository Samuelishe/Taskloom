using System.IO;

namespace Taskloom.Infrastructure.Storage;

/// <summary>
/// Вычисляет стандартные пути хранения файлов приложения.
/// </summary>
public static class TaskloomPaths
{
    public static string GetAppDataDirectoryPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Taskloom");
    }

    /// <summary>
    /// Возвращает путь к SQLite-файлу приложения в локальном профиле пользователя.
    /// </summary>
    public static string GetDatabasePath()
    {
        return Path.Combine(GetAppDataDirectoryPath(), "taskloom.db");
    }

    /// <summary>
    /// Возвращает путь к файлу настроек приложения.
    /// </summary>
    public static string GetSettingsPath()
    {
        return Path.Combine(GetAppDataDirectoryPath(), "settings.json");
    }

    /// <summary>
    /// Возвращает путь к файлу диагностического лога уведомлений.
    /// </summary>
    public static string GetNotificationLogPath()
    {
        return Path.Combine(GetAppDataDirectoryPath(), "notifications.log");
    }

    /// <summary>
    /// Возвращает путь к файлу диагностики импорта аудио.
    /// </summary>
    public static string GetAudioImportLogPath()
    {
        return Path.Combine(GetAppDataDirectoryPath(), "audio-import.log");
    }

    /// <summary>
    /// Возвращает путь к файлу диагностики ошибок запуска приложения.
    /// </summary>
    public static string GetStartupLogPath()
    {
        return Path.Combine(GetAppDataDirectoryPath(), "startup.log");
    }

    /// <summary>
    /// Возвращает путь к файлу диагностики ошибок загрузки записей.
    /// </summary>
    public static string GetRecordLoadLogPath()
    {
        return Path.Combine(GetAppDataDirectoryPath(), "record-load.log");
    }

    /// <summary>
    /// Возвращает путь к файлу диагностики ошибок сохранения записей.
    /// </summary>
    public static string GetRecordSaveLogPath()
    {
        return Path.Combine(GetAppDataDirectoryPath(), "record-save.log");
    }

    /// <summary>
    /// Возвращает путь к каталогу хранения пользовательских изображений.
    /// </summary>
    public static string GetImagesDirectoryPath()
    {
        return Path.Combine(GetAppDataDirectoryPath(), "Media", "Images");
    }

    /// <summary>
    /// Возвращает путь к каталогу хранения пользовательских аудиофайлов.
    /// </summary>
    public static string GetAudioDirectoryPath()
    {
        return Path.Combine(GetAppDataDirectoryPath(), "Media", "Audio");
    }

    /// <summary>
    /// Возвращает путь к каталогу хранения обложек аудиофайлов.
    /// </summary>
    public static string GetAudioCoversDirectoryPath()
    {
        return Path.Combine(GetAppDataDirectoryPath(), "Media", "AudioCovers");
    }
}
