using Taskloom.Domain;
using Taskloom.Services.Localization;
using Taskloom.Services.Notifications;
using Taskloom.Services.Records;

namespace Taskloom.Infrastructure.Notifications;

/// <summary>
/// Показывает Windows-напоминания о календарных записях через выбранный транспорт уведомлений.
/// </summary>
public sealed class WindowsBalloonEventReminderService : IEventReminderService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan NotificationWindow = TimeSpan.FromSeconds(45);

    private readonly ICalendarRecordService _recordService;
    private readonly ILocalizationService _localizationService;
    private readonly IAppNotificationService _notificationService;
    private readonly HashSet<string> _shownNotificationKeys = new(StringComparer.Ordinal);
    private readonly System.Threading.Timer _timer;
    private bool _isChecking;
    private bool _notificationErrorShown;

    public WindowsBalloonEventReminderService(
        ICalendarRecordService recordService,
        ILocalizationService localizationService,
        IAppNotificationService notificationService)
    {
        _recordService = recordService ?? throw new ArgumentNullException(nameof(recordService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

        _timer = new System.Threading.Timer(CheckReminders, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public void Start()
    {
        _timer.Change(TimeSpan.FromSeconds(10), CheckInterval);
    }

    public void Stop()
    {
        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public void Dispose()
    {
        Stop();
        _timer.Dispose();
    }

    private async void CheckReminders(object? state)
    {
        if (_isChecking)
        {
            return;
        }

        _isChecking = true;

        try
        {
            await CheckRemindersAsync();
        }
        catch (Exception exception)
        {
            ShowNotificationError(exception);
        }
        finally
        {
            _isChecking = false;
        }
    }

    private async Task CheckRemindersAsync()
    {
        var now = DateTime.Now;

        for (var dayOffset = 0; dayOffset <= 1; dayOffset++)
        {
            var date = DateOnly.FromDateTime(now.Date.AddDays(dayOffset));
            var eventRecords = await _recordService.GetRecordsByDateAsync(date, RecordType.Event);
            var taskRecords = await _recordService.GetRecordsByDateAsync(date, RecordType.Task);

            foreach (var eventRecord in eventRecords.OfType<EventRecord>())
            {
                TryShowReminderNotification(eventRecord, now);
                TryShowStartedNotification(eventRecord, now);
            }

            foreach (var taskRecord in taskRecords.OfType<TaskRecord>())
            {
                TryShowTaskReminderNotification(taskRecord, now);
            }
        }
    }

    private void TryShowReminderNotification(EventRecord eventRecord, DateTime now)
    {
        if (eventRecord.Status != EventStatus.Scheduled)
        {
            return;
        }

        var eventStart = eventRecord.Date.ToDateTime(eventRecord.StartTime);
        var reminderTime = eventStart.AddMinutes(-eventRecord.ReminderMinutesBefore);

        if (!IsWithinNotificationWindow(now, reminderTime))
        {
            return;
        }

        var reminderKey = $"reminder:{eventRecord.Id:D}:{eventStart:O}:{eventRecord.ReminderMinutesBefore}";

        if (!_shownNotificationKeys.Add(reminderKey))
        {
            return;
        }

        var title = _localizationService.GetString("Notification.EventReminderTitle");
        var message = _localizationService.Format(
            "Notification.EventReminderMessage",
            eventRecord.Title,
            eventStart.ToString("HH:mm"));

        _notificationService.ShowInfo(title, message);
    }

    private void TryShowStartedNotification(EventRecord eventRecord, DateTime now)
    {
        if (eventRecord.Status != EventStatus.Scheduled)
        {
            return;
        }

        var eventStart = eventRecord.Date.ToDateTime(eventRecord.StartTime);

        if (!IsWithinNotificationWindow(now, eventStart))
        {
            return;
        }

        var startedKey = $"started:{eventRecord.Id:D}:{eventStart:O}";

        if (!_shownNotificationKeys.Add(startedKey))
        {
            return;
        }

        var title = _localizationService.GetString("Notification.EventStartedTitle");
        var message = _localizationService.Format(
            "Notification.EventStartedMessage",
            eventRecord.Title,
            eventStart.ToString("HH:mm"));

        _notificationService.ShowInfo(title, message);
    }

    private void TryShowTaskReminderNotification(TaskRecord taskRecord, DateTime now)
    {
        if (taskRecord.IsCompleted || taskRecord.ReminderTime is null)
        {
            return;
        }

        var reminderTime = taskRecord.Date.ToDateTime(taskRecord.ReminderTime.Value);

        if (!IsWithinNotificationWindow(now, reminderTime))
        {
            return;
        }

        var reminderKey = $"task:{taskRecord.Id:D}:{reminderTime:O}";

        if (!_shownNotificationKeys.Add(reminderKey))
        {
            return;
        }

        var title = _localizationService.GetString("Notification.TaskReminderTitle");
        var message = _localizationService.Format(
            "Notification.TaskReminderMessage",
            taskRecord.Title,
            reminderTime.ToString("HH:mm"));

        _notificationService.ShowInfo(title, message);
    }

    private static bool IsWithinNotificationWindow(DateTime now, DateTime targetTime)
    {
        return now >= targetTime && now < targetTime + NotificationWindow;
    }

    private void ShowNotificationError(Exception exception)
    {
        if (_notificationErrorShown)
        {
            return;
        }

        _notificationErrorShown = true;
        _notificationService.ShowWarning(
            _localizationService.GetString("App.Title"),
            exception.Message);
    }
}
