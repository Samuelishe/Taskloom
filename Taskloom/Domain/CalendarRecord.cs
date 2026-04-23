namespace Taskloom.Domain;

/// <summary>
/// Базовая сущность календарной записи.
/// </summary>
public abstract class CalendarRecord
{
    protected CalendarRecord(Guid id, RecordType type, DateOnly date, string title, string? details)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Type = type;
        Date = date;
        Title = NormalizeRequiredText(title, nameof(title), 200);
        Details = NormalizeOptionalText(details, 4000);
    }

    public Guid Id { get; }

    public RecordType Type { get; }

    public DateOnly Date { get; private set; }

    public string Title { get; private set; }

    public string? Details { get; private set; }

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
}
