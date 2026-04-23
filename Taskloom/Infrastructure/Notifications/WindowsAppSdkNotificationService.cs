using System.IO;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Taskloom.Infrastructure.Storage;
using Taskloom.Services.Notifications;

namespace Taskloom.Infrastructure.Notifications;

/// <summary>
/// Показывает локальные уведомления через Windows App SDK и сохраняет их в центре уведомлений.
/// </summary>
public sealed class WindowsAppSdkNotificationService : IAppNotificationService
{
    private const string NotificationGroup = "taskloom";

    private readonly IAppNotificationService _fallbackNotificationService;
    private readonly Action? _notificationActivated;
    private readonly AppNotificationManager _notificationManager;
    private readonly string _logPath;
    private bool _isRegistered;
    private bool _useFallback;

    public WindowsAppSdkNotificationService(
        IAppNotificationService fallbackNotificationService,
        Action? notificationActivated = null)
    {
        _fallbackNotificationService = fallbackNotificationService ?? throw new ArgumentNullException(nameof(fallbackNotificationService));
        _notificationActivated = notificationActivated;
        _logPath = TaskloomPaths.GetNotificationLogPath();

        _notificationManager = AppNotificationManager.Default;

        try
        {
            if (!AppNotificationManager.IsSupported())
            {
                WriteDiagnostic("Windows App SDK notifications are not supported for the current process.");
                _useFallback = true;
                return;
            }

            _notificationManager.NotificationInvoked += OnNotificationInvoked;
            _notificationManager.Register();

            _isRegistered = true;
            WriteDiagnostic("Windows App SDK notifications registered successfully.");
        }
        catch (Exception exception)
        {
            _notificationManager.NotificationInvoked -= OnNotificationInvoked;
            WriteDiagnostic($"Registration failed. {exception}");
            _useFallback = true;
        }
    }

    public void ShowInfo(string title, string message)
    {
        Show(title, message, AppNotificationPriority.Default);
    }

    public void ShowWarning(string title, string message)
    {
        Show(title, message, AppNotificationPriority.High);
    }

    public void Dispose()
    {
        if (!_isRegistered)
        {
            _fallbackNotificationService.Dispose();
            return;
        }

        try
        {
            _notificationManager.NotificationInvoked -= OnNotificationInvoked;
            _notificationManager.Unregister();
            WriteDiagnostic("Windows App SDK notifications unregistered.");
        }
        catch (Exception exception)
        {
            WriteDiagnostic($"Unregister failed. {exception}");
        }
        finally
        {
            _fallbackNotificationService.Dispose();
        }
    }

    private void Show(string title, string message, AppNotificationPriority priority)
    {
        if (_useFallback)
        {
            ShowFallback(title, message, priority);
            return;
        }

        try
        {
            var notification = new AppNotificationBuilder()
                .AddArgument("action", "open")
                .AddText(title)
                .AddText(message)
                .SetGroup(NotificationGroup)
                .SetTag(Guid.NewGuid().ToString("N"))
                .BuildNotification();

            notification.Priority = priority;
            notification.Expiration = DateTimeOffset.Now.AddDays(7);

            _notificationManager.Show(notification);
            WriteDiagnostic($"Notification shown via App SDK. Title='{title}'.");
        }
        catch (Exception exception)
        {
            WriteDiagnostic($"Show failed. {exception}");
            _useFallback = true;
            ShowFallback(title, message, priority);
        }
    }

    private void ShowFallback(string title, string message, AppNotificationPriority priority)
    {
        if (priority == AppNotificationPriority.High)
        {
            WriteDiagnostic($"Fallback warning notification used. Title='{title}'.");
            _fallbackNotificationService.ShowWarning(title, message);
            return;
        }

        WriteDiagnostic($"Fallback info notification used. Title='{title}'.");
        _fallbackNotificationService.ShowInfo(title, message);
    }

    private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        WriteDiagnostic("Notification activated by user.");
        _notificationActivated?.Invoke();
    }

    private void WriteDiagnostic(string message)
    {
        try
        {
            var directoryPath = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.AppendAllText(
                _logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch
        {
            // Ошибки диагностики не должны ломать показ уведомлений.
        }
    }
}
