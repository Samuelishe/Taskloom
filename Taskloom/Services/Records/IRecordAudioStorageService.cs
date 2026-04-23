using Taskloom.Domain;

namespace Taskloom.Services.Records;

/// <summary>
/// Описывает хранение аудиофайлов записей и извлечение их метаданных.
/// </summary>
public interface IRecordAudioStorageService
{
    Task<RecordAudioDraft> PrepareAudioDraftAsync(
        string sourceFilePath,
        int sortOrder,
        CancellationToken cancellationToken = default);

    Task<RecordAttachment> ImportAudioAsync(
        Guid recordId,
        RecordAudioDraft draft,
        CancellationToken cancellationToken = default);

    string? GetAbsolutePath(string? relativePath);

    void DeleteIfExists(string? relativePath);
}
