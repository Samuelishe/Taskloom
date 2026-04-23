namespace Taskloom.Domain;

/// <summary>
/// Событие с обязательным временным диапазоном.
/// </summary>
public sealed class EventRecord : CalendarRecord
{
    public EventRecord(
        Guid id,
        DateOnly date,
        string title,
        string? details,
        TimeOnly startTime,
        TimeOnly endTime,
        string? location = null,
        EventStatus status = EventStatus.Scheduled,
        int reminderMinutesBefore = 60)
        : base(id, RecordType.Event, date, title, details)
    {
        SetSchedule(startTime, endTime);
        Location = NormalizeOptionalText(location, 300);
        Status = status;
        SetReminder(reminderMinutesBefore);
    }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    public string? Location { get; private set; }

    public EventStatus Status { get; private set; }

    public int ReminderMinutesBefore { get; private set; }

    public void SetSchedule(TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
        {
            throw new ArgumentOutOfRangeException(nameof(endTime), "Время окончания должно быть больше времени начала.");
        }

        StartTime = startTime;
        EndTime = endTime;
    }

    public void ChangeLocation(string? location)
    {
        Location = NormalizeOptionalText(location, 300);
    }

    public void ChangeStatus(EventStatus status)
    {
        Status = status;
    }

    public void SetReminder(int minutesBefore)
    {
        if (minutesBefore < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minutesBefore), "Время напоминания не может быть отрицательным.");
        }

        ReminderMinutesBefore = minutesBefore;
    }
}
