namespace Taskloom.Domain;

/// <summary>
/// Статус события в календаре.
/// </summary>
public enum EventStatus
{
    Scheduled = 1,
    Completed = 2,
    Rescheduled = 3,
    Canceled = 4
}
