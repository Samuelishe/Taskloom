using Taskloom.Domain;

namespace Taskloom.Services.Records;

/// <summary>
/// Описывает синхронизацию metadata-файла и каталога ресурсов записи.
/// </summary>
public interface IRecordResourceMetadataService
{
    Task WriteRecordMetadataAsync(CalendarRecord record, CancellationToken cancellationToken = default);

    void DeleteRecordResources(Guid recordId);
}
