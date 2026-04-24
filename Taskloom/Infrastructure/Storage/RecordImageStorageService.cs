using System.IO;
using Taskloom.Domain;
using Taskloom.Services.Records;

namespace Taskloom.Infrastructure.Storage;

/// <summary>
/// Хранит прикреплённые изображения записей в локальном профиле пользователя.
/// </summary>
public sealed class RecordImageStorageService : IRecordImageStorageService
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".bmp"] = "image/bmp",
        [".gif"] = "image/gif"
    };

    public async Task<RecordAttachment> ImportImageAsync(
        Guid recordId,
        string sourceFilePath,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);

        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Не найден файл изображения.", sourceFilePath);
        }

        var extension = Path.GetExtension(sourceFilePath);

        if (!ContentTypes.TryGetValue(extension, out var contentType))
        {
            throw new InvalidOperationException("Поддерживаются только изображения JPEG, PNG, BMP и GIF.");
        }

        var targetDirectory = TaskloomPaths.GetRecordImagesDirectoryPath(recordId);
        Directory.CreateDirectory(targetDirectory);

        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var targetPath = Path.Combine(targetDirectory, storedFileName);

        await using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        await using (var targetStream = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await sourceStream.CopyToAsync(targetStream, cancellationToken);
        }

        var sourceInfo = new FileInfo(sourceFilePath);
        var relativePath = Path.Combine("Records", recordId.ToString("D"), "images", storedFileName);

        return new RecordAttachment(
            Guid.NewGuid(),
            recordId,
            RecordAttachmentKind.Image,
            sourceInfo.Name,
            storedFileName,
            relativePath,
            contentType,
            sourceInfo.Length,
            DateTime.UtcNow,
            sortOrder);
    }

    public string? GetAbsolutePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        return Path.Combine(TaskloomPaths.GetAppDataDirectoryPath(), relativePath);
    }

    public void DeleteIfExists(string? relativePath)
    {
        var absolutePath = GetAbsolutePath(relativePath);

        if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
        {
            return;
        }

        File.Delete(absolutePath);
        CleanupEmptyRecordDirectories(absolutePath);
    }

    private static void CleanupEmptyRecordDirectories(string deletedAbsolutePath)
    {
        try
        {
            var recordsDirectoryPath = TaskloomPaths.GetRecordsDirectoryPath();
            var currentDirectoryPath = Path.GetDirectoryName(deletedAbsolutePath);

            while (!string.IsNullOrWhiteSpace(currentDirectoryPath) &&
                   currentDirectoryPath.StartsWith(recordsDirectoryPath, StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(currentDirectoryPath, recordsDirectoryPath, StringComparison.OrdinalIgnoreCase))
            {
                if (Directory.EnumerateFileSystemEntries(currentDirectoryPath).Any())
                {
                    break;
                }

                var parentDirectoryPath = Path.GetDirectoryName(currentDirectoryPath);
                Directory.Delete(currentDirectoryPath, false);
                currentDirectoryPath = parentDirectoryPath;
            }
        }
        catch
        {
            // Ошибки фоновой очистки пустых каталогов не должны ломать удаление файла.
        }
    }
}
