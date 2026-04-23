using System.Windows.Forms;
using Taskloom.Services.Notifications;

namespace Taskloom.Infrastructure.Notifications;

/// <summary>
/// Временный транспорт уведомлений через tray balloon.
/// </summary>
public sealed class WindowsBalloonAppNotificationService : IAppNotificationService
{
    private readonly NotifyIcon _notifyIcon;

    public WindowsBalloonAppNotificationService(NotifyIcon notifyIcon)
    {
        _notifyIcon = notifyIcon ?? throw new ArgumentNullException(nameof(notifyIcon));
    }

    public void ShowInfo(string title, string message)
    {
        _notifyIcon.ShowBalloonTip(10000, title, message, ToolTipIcon.Info);
    }

    public void ShowWarning(string title, string message)
    {
        _notifyIcon.ShowBalloonTip(10000, title, message, ToolTipIcon.Warning);
    }

    public void Dispose()
    {
    }
}
