using Taskloom.Domain;
using Taskloom.Services.Settings;

namespace Taskloom.Services.Records;

/// <summary>
/// Реализует прикладные сценарии создания, редактирования, удаления и фильтрации записей.
/// </summary>
public sealed class CalendarRecordService : ICalendarRecordService
{
    private readonly ICalendarRecordRepository _repository;
    private readonly IRecordImageStorageService _imageStorageService;
    private readonly IRecordAudioStorageService _audioStorageService;
    private readonly IRecordResourceMetadataService _recordResourceMetadataService;

    public CalendarRecordService(
        ICalendarRecordRepository repository,
        IRecordImageStorageService imageStorageService,
        IRecordAudioStorageService audioStorageService,
        IRecordResourceMetadataService recordResourceMetadataService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _imageStorageService = imageStorageService ?? throw new ArgumentNullException(nameof(imageStorageService));
        _audioStorageService = audioStorageService ?? throw new ArgumentNullException(nameof(audioStorageService));
        _recordResourceMetadataService = recordResourceMetadataService ?? throw new ArgumentNullException(nameof(recordResourceMetadataService));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CalendarRecord>> GetRecordsByDateAsync(
        DateOnly date,
        RecordType? type = null,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetByDateAsync(date, type, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CalendarRecordDraft?> GetDraftByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _repository.GetByIdAsync(id, cancellationToken);
        return record is null ? null : MapToDraft(record);
    }

    /// <inheritdoc />
    public async Task<CalendarRecord> SaveAsync(CalendarRecordDraft draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var existingRecord = draft.Id.HasValue
            ? await _repository.GetByIdAsync(draft.Id.Value, cancellationToken)
            : null;

        var (record, importedRelativePaths) = await CreateRecordAsync(draft, existingRecord, cancellationToken);

        try
        {
            await _repository.SaveAsync(record, cancellationToken);
        }
        catch
        {
            foreach (var relativePath in importedRelativePaths)
            {
                DeleteImportedAttachmentPath(relativePath);
            }

            throw;
        }

        DeleteRemovedImages(existingRecord?.ImageAttachments ?? [], record.ImageAttachments);
        DeleteRemovedAudios(existingRecord?.AudioAttachments ?? [], record.AudioAttachments);
        await _recordResourceMetadataService.WriteRecordMetadataAsync(record, cancellationToken);

        return record;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existingRecord = await _repository.GetByIdAsync(id, cancellationToken);
        await _repository.DeleteAsync(id, cancellationToken);

        foreach (var attachment in existingRecord?.ImageAttachments ?? [])
        {
            _imageStorageService.DeleteIfExists(attachment.RelativePath);
        }

        foreach (var attachment in existingRecord?.AudioAttachments ?? [])
        {
            DeleteAudioAttachmentFiles(attachment);
        }

        _recordResourceMetadataService.DeleteRecordResources(id);
    }

    /// <inheritdoc />
    public async Task<TaskRecord> ToggleTaskCompletionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _repository.GetByIdAsync(id, cancellationToken);

        if (record is null)
        {
            throw new InvalidOperationException("Не удалось найти задачу для изменения статуса.");
        }

        if (record is not TaskRecord taskRecord)
        {
            throw new InvalidOperationException("Быстрое изменение статуса доступно только для задач.");
        }

        if (taskRecord.IsCompleted)
        {
            taskRecord.MarkPending();
        }
        else
        {
            taskRecord.MarkCompleted();
        }

        await _repository.SaveAsync(taskRecord, cancellationToken);
        return taskRecord;
    }

    /// <inheritdoc />
    public async Task<int> CleanupOldRecordsAsync(RecordCleanupMode cleanupMode, CancellationToken cancellationToken = default)
    {
        if (cleanupMode == RecordCleanupMode.Never)
        {
            return 0;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var cutoffDate = cleanupMode switch
        {
            RecordCleanupMode.DeleteAllOlderThan7Days => today.AddDays(-7),
            RecordCleanupMode.DeleteCompletedAndPastOlderThan7Days => today.AddDays(-7),
            RecordCleanupMode.DeleteAllOlderThan1Month => today.AddMonths(-1),
            RecordCleanupMode.DeleteCompletedAndPastOlderThan1Month => today.AddMonths(-1),
            _ => today
        };

        var records = await _repository.GetOlderThanAsync(cutoffDate, cancellationToken);
        var recordsToDelete = records
            .Where(record => ShouldDeleteRecord(record, cleanupMode, today))
            .ToArray();

        foreach (var record in recordsToDelete)
        {
            await DeleteAsync(record.Id, cancellationToken);
        }

        return recordsToDelete.Length;
    }

    private async Task<(CalendarRecord Record, List<string> ImportedRelativePaths)> CreateRecordAsync(
        CalendarRecordDraft draft,
        CalendarRecord? existingRecord,
        CancellationToken cancellationToken)
    {
        var id = draft.Id ?? Guid.NewGuid();

        CalendarRecord record = draft.Type switch
        {
            RecordType.Task => new TaskRecord(
                id,
                draft.Date,
                draft.Title,
                draft.Details,
                draft.IsCompleted,
                draft.TaskReminderTime,
                existingRecord?.CreatedUtc),

            RecordType.Note => new NoteRecord(
                id,
                draft.Date,
                draft.Title,
                draft.Details,
                existingRecord?.CreatedUtc),

            RecordType.Event => new EventRecord(
                id,
                draft.Date,
                draft.Title,
                draft.Details,
                GetRequiredTime(draft.StartTime, nameof(draft.StartTime)),
                GetRequiredTime(draft.EndTime, nameof(draft.EndTime)),
                draft.Location,
                draft.EventStatus,
                draft.ReminderMinutesBefore,
                existingRecord?.CreatedUtc),

            RecordType.DaySummary => new DaySummaryRecord(
                id,
                draft.Date,
                draft.Title,
                draft.Details,
                existingRecord?.CreatedUtc),

            _ => throw new InvalidOperationException($"Неподдерживаемый тип записи: {draft.Type}.")
        };

        var resolvedImages = await ResolveImageAttachmentsAsync(
            draft.Images,
            existingRecord?.ImageAttachments ?? [],
            id,
            cancellationToken);

        var resolvedAudios = await ResolveAudioAttachmentsAsync(
            draft.Audios,
            existingRecord?.AudioAttachments ?? [],
            id,
            cancellationToken);

        var attachments = resolvedImages.Attachments
            .Concat(resolvedAudios.Attachments)
            .OrderBy(static attachment => attachment.Kind)
            .ThenBy(static attachment => attachment.SortOrder)
            .ThenBy(static attachment => attachment.CreatedUtc)
            .ToArray();

        if (attachments.Length > 0)
        {
            record.ReplaceAttachments(attachments);
        }

        return (
            record,
            resolvedImages.ImportedRelativePaths
                .Concat(resolvedAudios.ImportedRelativePaths)
                .ToList());
    }

    private static CalendarRecordDraft MapToDraft(CalendarRecord record)
    {
        var draft = new CalendarRecordDraft
        {
            Id = record.Id,
            Type = record.Type,
            Date = record.Date,
            Title = record.Title,
            Details = record.Details,
            Images = record.ImageAttachments.Select(MapImageToDraft).ToList(),
            Audios = record.AudioAttachments.Select(MapAudioToDraft).ToList()
        };

        switch (record)
        {
            case TaskRecord taskRecord:
                draft.IsCompleted = taskRecord.IsCompleted;
                draft.TaskReminderTime = taskRecord.ReminderTime;
                break;

            case EventRecord eventRecord:
                draft.StartTime = eventRecord.StartTime;
                draft.EndTime = eventRecord.EndTime;
                draft.Location = eventRecord.Location;
                draft.EventStatus = eventRecord.Status;
                draft.ReminderMinutesBefore = eventRecord.ReminderMinutesBefore;
                break;
        }

        return draft;
    }

    private static TimeOnly GetRequiredTime(TimeOnly? value, string propertyName)
    {
        return value ?? throw new InvalidOperationException(
            $"Для записи типа Event обязательно значение {propertyName}.");
    }

    private async Task<(List<RecordAttachment> Attachments, List<string> ImportedRelativePaths)> ResolveImageAttachmentsAsync(
        IReadOnlyList<RecordImageDraft> imageDrafts,
        IReadOnlyList<RecordAttachment> existingAttachments,
        Guid recordId,
        CancellationToken cancellationToken)
    {
        var attachments = new List<RecordAttachment>();
        var importedRelativePaths = new List<string>();

        foreach (var imageDraft in imageDrafts.OrderBy(static image => image.SortOrder))
        {
            if (imageDraft.IsPendingImport)
            {
                var importedAttachment = await _imageStorageService.ImportImageAsync(
                    recordId,
                    imageDraft.SourceFilePath!,
                    imageDraft.SortOrder,
                    cancellationToken);

                attachments.Add(importedAttachment);
                importedRelativePaths.Add(importedAttachment.RelativePath);
                continue;
            }

            if (string.IsNullOrWhiteSpace(imageDraft.RelativePath))
            {
                continue;
            }

            var existingAttachment = existingAttachments.FirstOrDefault(attachment =>
                string.Equals(attachment.RelativePath, imageDraft.RelativePath, StringComparison.OrdinalIgnoreCase));

            attachments.Add(new RecordAttachment(
                existingAttachment?.Id ?? imageDraft.Id ?? Guid.NewGuid(),
                recordId,
                RecordAttachmentKind.Image,
                imageDraft.OriginalFileName,
                imageDraft.StoredFileName,
                imageDraft.RelativePath,
                imageDraft.ContentType,
                imageDraft.FileSize,
                imageDraft.CreatedUtc == default ? DateTime.UtcNow : imageDraft.CreatedUtc,
                imageDraft.SortOrder));
        }

        return (attachments, importedRelativePaths);
    }

    private async Task<(List<RecordAttachment> Attachments, List<string> ImportedRelativePaths)> ResolveAudioAttachmentsAsync(
        IReadOnlyList<RecordAudioDraft> audioDrafts,
        IReadOnlyList<RecordAttachment> existingAttachments,
        Guid recordId,
        CancellationToken cancellationToken)
    {
        var attachments = new List<RecordAttachment>();
        var importedRelativePaths = new List<string>();

        foreach (var audioDraft in audioDrafts.OrderBy(static audio => audio.SortOrder))
        {
            if (audioDraft.IsPendingImport)
            {
                var importedAttachment = await _audioStorageService.ImportAudioAsync(recordId, audioDraft, cancellationToken);

                attachments.Add(importedAttachment);
                importedRelativePaths.Add(importedAttachment.RelativePath);

                if (!string.IsNullOrWhiteSpace(importedAttachment.PreviewRelativePath))
                {
                    importedRelativePaths.Add(importedAttachment.PreviewRelativePath);
                }

                continue;
            }

            if (string.IsNullOrWhiteSpace(audioDraft.RelativePath))
            {
                continue;
            }

            var existingAttachment = existingAttachments.FirstOrDefault(attachment =>
                string.Equals(attachment.RelativePath, audioDraft.RelativePath, StringComparison.OrdinalIgnoreCase));

            attachments.Add(new RecordAttachment(
                existingAttachment?.Id ?? audioDraft.Id ?? Guid.NewGuid(),
                recordId,
                RecordAttachmentKind.Audio,
                audioDraft.OriginalFileName,
                audioDraft.StoredFileName,
                audioDraft.RelativePath,
                audioDraft.ContentType,
                audioDraft.FileSize,
                audioDraft.CreatedUtc == default ? DateTime.UtcNow : audioDraft.CreatedUtc,
                audioDraft.SortOrder,
                audioDraft.DisplayTitle,
                audioDraft.DurationSeconds,
                audioDraft.CoverRelativePath,
                audioDraft.AlbumTitle,
                audioDraft.Genre));
        }

        return (attachments, importedRelativePaths);
    }

    private void DeleteRemovedImages(
        IReadOnlyList<RecordAttachment> previousAttachments,
        IReadOnlyList<RecordAttachment> currentAttachments)
    {
        var currentPaths = currentAttachments
            .Select(attachment => attachment.RelativePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var previousAttachment in previousAttachments)
        {
            if (!currentPaths.Contains(previousAttachment.RelativePath))
            {
                _imageStorageService.DeleteIfExists(previousAttachment.RelativePath);
            }
        }
    }

    private void DeleteRemovedAudios(
        IReadOnlyList<RecordAttachment> previousAttachments,
        IReadOnlyList<RecordAttachment> currentAttachments)
    {
        var currentPaths = currentAttachments
            .Select(attachment => attachment.RelativePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var previousAttachment in previousAttachments)
        {
            if (!currentPaths.Contains(previousAttachment.RelativePath))
            {
                DeleteAudioAttachmentFiles(previousAttachment);
            }
        }
    }

    private void DeleteAudioAttachmentFiles(RecordAttachment attachment)
    {
        _audioStorageService.DeleteIfExists(attachment.RelativePath);
        _audioStorageService.DeleteIfExists(attachment.PreviewRelativePath);
    }

    private void DeleteImportedAttachmentPath(string relativePath)
    {
        if (relativePath.Contains("Audio", StringComparison.OrdinalIgnoreCase))
        {
            _audioStorageService.DeleteIfExists(relativePath);
            return;
        }

        _imageStorageService.DeleteIfExists(relativePath);
    }

    private static RecordImageDraft MapImageToDraft(RecordAttachment attachment)
    {
        return new RecordImageDraft
        {
            Id = attachment.Id,
            OriginalFileName = attachment.OriginalFileName,
            StoredFileName = attachment.StoredFileName,
            RelativePath = attachment.RelativePath,
            ContentType = attachment.ContentType,
            FileSize = attachment.FileSize,
            CreatedUtc = attachment.CreatedUtc,
            SortOrder = attachment.SortOrder,
            PreviewPath = null
        };
    }

    private static RecordAudioDraft MapAudioToDraft(RecordAttachment attachment)
    {
        return new RecordAudioDraft
        {
            Id = attachment.Id,
            OriginalFileName = attachment.OriginalFileName,
            StoredFileName = attachment.StoredFileName,
            RelativePath = attachment.RelativePath,
            ContentType = attachment.ContentType,
            FileSize = attachment.FileSize,
            CreatedUtc = attachment.CreatedUtc,
            SortOrder = attachment.SortOrder,
            DisplayTitle = attachment.DisplayTitle,
            DurationSeconds = attachment.DurationSeconds,
            CoverRelativePath = attachment.PreviewRelativePath,
            AlbumTitle = attachment.AlbumTitle,
            Genre = attachment.Genre
        };
    }

    private static bool ShouldDeleteRecord(CalendarRecord record, RecordCleanupMode cleanupMode, DateOnly today)
    {
        return cleanupMode switch
        {
            RecordCleanupMode.DeleteAllOlderThan7Days => true,
            RecordCleanupMode.DeleteAllOlderThan1Month => true,
            RecordCleanupMode.DeleteCompletedAndPastOlderThan7Days => record switch
            {
                TaskRecord taskRecord => taskRecord.IsCompleted,
                EventRecord eventRecord => eventRecord.Date < today,
                _ => false
            },
            RecordCleanupMode.DeleteCompletedAndPastOlderThan1Month => record switch
            {
                TaskRecord taskRecord => taskRecord.IsCompleted,
                EventRecord eventRecord => eventRecord.Date < today,
                _ => false
            },
            _ => false
        };
    }
}
