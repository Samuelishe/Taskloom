using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Taskloom.Services.Localization;

namespace Taskloom.Infrastructure.Tray;

/// <summary>
/// Управляет иконкой приложения в системном трее.
/// </summary>
public sealed class WindowsTrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _icon;

    public WindowsTrayService(ILocalizationService localizationService)
    {
        ArgumentNullException.ThrowIfNull(localizationService);

        _icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "Taskloom.ico"));

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Открыть", null, (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty));
        contextMenu.Items.Add("Настройки", null, (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty));
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Выход", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = contextMenu,
            Icon = _icon,
            Text = localizationService.GetString("App.Title"),
            Visible = true
        };

        _notifyIcon.DoubleClick += OnNotifyIconDoubleClick;
    }

    public event EventHandler? OpenRequested;

    public event EventHandler? SettingsRequested;

    public event EventHandler? ExitRequested;

    public NotifyIcon NotifyIcon => _notifyIcon;

    public void Dispose()
    {
        _notifyIcon.DoubleClick -= OnNotifyIconDoubleClick;
        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
        _icon.Dispose();
    }

    private void OnNotifyIconDoubleClick(object? sender, EventArgs e)
    {
        OpenRequested?.Invoke(this, EventArgs.Empty);
    }
}
