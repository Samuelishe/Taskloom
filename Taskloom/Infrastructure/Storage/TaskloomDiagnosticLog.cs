using System.IO;

namespace Taskloom.Infrastructure.Storage;

/// <summary>
/// Пишет диагностические сообщения приложения в локальные лог-файлы.
/// </summary>
public static class TaskloomDiagnosticLog
{
    public static void Append(string logPath, string message)
    {
        try
        {
            var directoryPath = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.AppendAllText(
                logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch
        {
            // Диагностическое логирование не должно ломать основной сценарий.
        }
    }

    public static void AppendException(string logPath, Exception exception, string? context = null)
    {
        var prefix = string.IsNullOrWhiteSpace(context) ? string.Empty : $"{context}{Environment.NewLine}";
        Append(logPath, $"{prefix}{exception}");
    }
}
