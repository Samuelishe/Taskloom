using System.IO;
using TagLib;
using Taskloom.Domain;
using Taskloom.Services.Records;

namespace Taskloom.Infrastructure.Storage;

/// <summary>
/// Хранит прикреплённые аудиофайлы и их обложки в локальном профиле пользователя.
/// </summary>
public sealed class RecordAudioStorageService : IRecordAudioStorageService
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".m4a"] = "audio/mp4",
        [".flac"] = "audio/flac",
        [".wma"] = "audio/x-ms-wma",
        [".ogg"] = "audio/ogg"
    };

    public Task<RecordAudioDraft> PrepareAudioDraftAsync(
        string sourceFilePath,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);

        if (!System.IO.File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Не найден аудиофайл.", sourceFilePath);
        }

        var extension = Path.GetExtension(sourceFilePath);

        if (!ContentTypes.TryGetValue(extension, out var contentType))
        {
            throw new InvalidOperationException("Поддерживаются только аудиофайлы MP3, WAV, M4A, FLAC, WMA и OGG.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var fileInfo = new FileInfo(sourceFilePath);
        string title = fileInfo.Name;
        double? durationSeconds = null;
        byte[]? coverBytes = null;
        string? albumTitle = null;
        string? genre = null;

        try
        {
            using var tagFile = TagLib.File.Create(sourceFilePath);
            title = string.IsNullOrWhiteSpace(tagFile.Tag?.Title)
                ? fileInfo.Name
                : tagFile.Tag.Title.Trim();
            albumTitle = string.IsNullOrWhiteSpace(tagFile.Tag?.Album)
                ? null
                : tagFile.Tag.Album.Trim();
            genre = tagFile.Tag?.Genres?
                .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))?
                .Trim();
            coverBytes = tagFile.Tag?.Pictures?.FirstOrDefault()?.Data?.Data;
            durationSeconds = tagFile.Properties?.Duration.TotalSeconds > 0
                ? tagFile.Properties.Duration.TotalSeconds
                : null;
        }
        catch
        {
            // Если метаданные не удалось прочитать, импорт всё равно должен продолжиться.
        }

        return Task.FromResult(new RecordAudioDraft
        {
            OriginalFileName = fileInfo.Name,
            SourceFilePath = sourceFilePath,
            ContentType = contentType,
            FileSize = fileInfo.Length,
            SortOrder = sortOrder,
            DisplayTitle = title,
            DurationSeconds = durationSeconds,
            AlbumTitle = albumTitle,
            Genre = genre,
            CoverBytes = coverBytes
        });
    }

    public async Task<RecordAttachment> ImportAudioAsync(
        Guid recordId,
        RecordAudioDraft draft,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (string.IsNullOrWhiteSpace(draft.SourceFilePath))
        {
            throw new InvalidOperationException("Для импорта аудиофайла нужен исходный путь.");
        }

        var sourceFilePath = draft.SourceFilePath;

        if (!System.IO.File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Не найден аудиофайл.", sourceFilePath);
        }

        var extension = Path.GetExtension(sourceFilePath);

        if (!ContentTypes.TryGetValue(extension, out var contentType))
        {
            throw new InvalidOperationException("Поддерживаются только аудиофайлы MP3, WAV, M4A, FLAC, WMA и OGG.");
        }

        var audioDirectory = TaskloomPaths.GetRecordAudioDirectoryPath(recordId);
        Directory.CreateDirectory(audioDirectory);

        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var targetPath = Path.Combine(audioDirectory, storedFileName);

        await using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        await using (var targetStream = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await sourceStream.CopyToAsync(targetStream, cancellationToken);
        }

        string? previewRelativePath = null;

        if (draft.CoverBytes is { Length: > 0 })
        {
            var coversDirectory = TaskloomPaths.GetRecordAudioCoversDirectoryPath(recordId);
            Directory.CreateDirectory(coversDirectory);

            var coverFileName = $"{Guid.NewGuid():N}.jpg";
            var coverPath = Path.Combine(coversDirectory, coverFileName);
            await System.IO.File.WriteAllBytesAsync(coverPath, draft.CoverBytes, cancellationToken);
            previewRelativePath = Path.Combine("Records", recordId.ToString("D"), "audio-covers", coverFileName);
        }

        var sourceInfo = new FileInfo(sourceFilePath);
        var relativePath = Path.Combine("Records", recordId.ToString("D"), "audio", storedFileName);

        return new RecordAttachment(
            Guid.NewGuid(),
            recordId,
            RecordAttachmentKind.Audio,
            sourceInfo.Name,
            storedFileName,
            relativePath,
            contentType,
            sourceInfo.Length,
            DateTime.UtcNow,
            draft.SortOrder,
            draft.DisplayTitle,
            draft.DurationSeconds,
            previewRelativePath,
            draft.AlbumTitle,
            draft.Genre);
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

        if (string.IsNullOrWhiteSpace(absolutePath) || !System.IO.File.Exists(absolutePath))
        {
            return;
        }

        System.IO.File.Delete(absolutePath);
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
