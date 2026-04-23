using Taskloom.Domain;

namespace Taskloom.Services.Records;

/// <summary>
/// Описывает файловое хранилище изображений календарных записей.
/// </summary>
public interface IRecordImageStorageService
{
    Task<RecordAttachment> ImportImageAsync(
        Guid recordId,
        string sourceFilePath,
        int sortOrder,
        CancellationToken cancellationToken = default);

    string? GetAbsolutePath(string? relativePath);

    void DeleteIfExists(string? relativePath);
}
