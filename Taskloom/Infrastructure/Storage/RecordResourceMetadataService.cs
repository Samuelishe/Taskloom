using System.IO;
using System.Text.Json;
using Taskloom.Domain;
using Taskloom.Services.Records;

namespace Taskloom.Infrastructure.Storage;

/// <summary>
/// Поддерживает metadata-файл и каталог ресурсов отдельной записи.
/// </summary>
public sealed class RecordResourceMetadataService : IRecordResourceMetadataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public async Task WriteRecordMetadataAsync(CalendarRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var recordDirectoryPath = TaskloomPaths.GetRecordDirectoryPath(record.Id);
        Directory.CreateDirectory(recordDirectoryPath);

        var metadata = new RecordResourceMetadata(
            record.Id,
            record.Type.ToString(),
            record.Date.ToString("yyyy-MM-dd"),
            record.Title,
            DateTime.UtcNow,
            record.ImageAttachments.Select(attachment => RecordResourceMetadataItem.FromAttachment(attachment)).ToArray(),
            record.AudioAttachments.Select(attachment => RecordResourceMetadataItem.FromAttachment(attachment)).ToArray());

        await using var stream = File.Create(TaskloomPaths.GetRecordMetadataPath(record.Id));
        await JsonSerializer.SerializeAsync(stream, metadata, SerializerOptions, cancellationToken);
    }

    public void DeleteRecordResources(Guid recordId)
    {
        var recordDirectoryPath = TaskloomPaths.GetRecordDirectoryPath(recordId);

        if (!Directory.Exists(recordDirectoryPath))
        {
            return;
        }

        Directory.Delete(recordDirectoryPath, true);
    }

    private sealed record RecordResourceMetadata(
        Guid RecordId,
        string RecordType,
        string RecordDate,
        string Title,
        DateTime MetadataGeneratedUtc,
        IReadOnlyList<RecordResourceMetadataItem> Images,
        IReadOnlyList<RecordResourceMetadataItem> Audio);

    private sealed record RecordResourceMetadataItem(
        string Kind,
        string OriginalFileName,
        string StoredFileName,
        string RelativePath,
        string? PreviewRelativePath,
        long FileSize,
        DateTime CreatedUtc)
    {
        public static RecordResourceMetadataItem FromAttachment(RecordAttachment attachment)
        {
            return new RecordResourceMetadataItem(
                attachment.Kind.ToString(),
                attachment.OriginalFileName,
                attachment.StoredFileName,
                attachment.RelativePath,
                attachment.PreviewRelativePath,
                attachment.FileSize,
                attachment.CreatedUtc);
        }
    }
}
