namespace Taskloom.Domain;

/// <summary>
/// Базовая сущность календарной записи.
/// </summary>
public abstract class CalendarRecord
{
    private List<RecordAttachment> _attachments = [];

    protected CalendarRecord(
        Guid id,
        RecordType type,
        DateOnly date,
        string title,
        string? details,
        int sortOrder = 0,
        bool hideLinksWhenPreviewAvailable = false,
        DateTime? createdUtc = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Type = type;
        Date = date;
        Title = NormalizeRequiredText(title, nameof(title), 200);
        Details = NormalizeOptionalText(details, 4000);
        SortOrder = NormalizeSortOrder(sortOrder);
        HideLinksWhenPreviewAvailable = hideLinksWhenPreviewAvailable;
        CreatedUtc = NormalizeUtc(createdUtc ?? DateTime.UtcNow);
    }

    public Guid Id { get; }

    public RecordType Type { get; }

    public DateOnly Date { get; private set; }

    public string Title { get; private set; }

    public string? Details { get; private set; }

    public int SortOrder { get; private set; }

    public bool HideLinksWhenPreviewAvailable { get; private set; }

    public DateTime CreatedUtc { get; }

    public IReadOnlyList<RecordAttachment> Attachments => _attachments;

    public RecordAttachment? PrimaryImageAttachment => _attachments.FirstOrDefault(static attachment => attachment.Kind == RecordAttachmentKind.Image);

    public IReadOnlyList<RecordAttachment> ImageAttachments => _attachments
        .Where(static attachment => attachment.Kind == RecordAttachmentKind.Image)
        .OrderBy(static attachment => attachment.SortOrder)
        .ThenBy(static attachment => attachment.CreatedUtc)
        .ToArray();

    public IReadOnlyList<RecordAttachment> AudioAttachments => _attachments
        .Where(static attachment => attachment.Kind == RecordAttachmentKind.Audio)
        .OrderBy(static attachment => attachment.SortOrder)
        .ThenBy(static attachment => attachment.CreatedUtc)
        .ToArray();

    public void Reschedule(DateOnly date)
    {
        Date = date;
    }

    public void Rename(string title)
    {
        Title = NormalizeRequiredText(title, nameof(title), 200);
    }

    public void ChangeDetails(string? details)
    {
        Details = NormalizeOptionalText(details, 4000);
    }

    public void SetSortOrder(int value)
    {
        SortOrder = NormalizeSortOrder(value);
    }

    public void SetHideLinksWhenPreviewAvailable(bool value)
    {
        HideLinksWhenPreviewAvailable = value;
    }

    public void ReplaceAttachments(IEnumerable<RecordAttachment>? attachments)
    {
        if (attachments is null)
        {
            _attachments = [];
            return;
        }

        var normalizedAttachments = attachments.ToList();

        if (normalizedAttachments.Any(attachment => attachment.RecordId != Id))
        {
            throw new InvalidOperationException("Вложение должно принадлежать той же записи, что и агрегат.");
        }

        _attachments = normalizedAttachments
            .OrderBy(static attachment => attachment.SortOrder)
            .ThenBy(static attachment => attachment.CreatedUtc)
            .ToList();
    }

    protected static string NormalizeRequiredText(string value, string paramName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, $"Значение не должно превышать {maxLength} символов.");
        }

        return normalizedValue;
    }

    protected static string? NormalizeOptionalText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Значение не должно превышать {maxLength} символов.");
        }

        return normalizedValue;
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();
    }

    private static int NormalizeSortOrder(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Порядок записи не может быть отрицательным.");
        }

        return value;
    }
}
