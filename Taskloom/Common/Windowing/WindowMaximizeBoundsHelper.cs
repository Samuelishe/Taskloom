using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;

namespace Taskloom.Common.Windowing;

/// <summary>
/// Корректирует границы развёрнутого безрамочного WPF-окна по рабочей области монитора.
/// </summary>
public static class WindowMaximizeBoundsHelper
{
    private const int WmGetMinMaxInfo = 0x0024;
    private const int MonitorDefaultToNearest = 0x00000002;

    public static void Attach(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        window.SourceInitialized += OnWindowSourceInitialized;
    }

    private static void OnWindowSourceInitialized(object? sender, EventArgs e)
    {
        if (sender is not Window window)
        {
            return;
        }

        window.SourceInitialized -= OnWindowSourceInitialized;

        var source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
        source?.AddHook(WindowProcedure);
    }

    private static IntPtr WindowProcedure(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != WmGetMinMaxInfo)
        {
            return IntPtr.Zero;
        }

        ApplyMaximizedBounds(hwnd, lParam);
        handled = true;
        return IntPtr.Zero;
    }

    private static void ApplyMaximizedBounds(IntPtr hwnd, IntPtr lParam)
    {
        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);

        if (monitor == IntPtr.Zero)
        {
            return;
        }

        var monitorInfo = new MonitorInfo
        {
            Size = Marshal.SizeOf<MonitorInfo>()
        };

        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            return;
        }

        var minMaxInfo = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        var workArea = monitorInfo.WorkArea;
        var monitorArea = monitorInfo.MonitorArea;

        minMaxInfo.MaxPosition.X = workArea.Left - monitorArea.Left;
        minMaxInfo.MaxPosition.Y = workArea.Top - monitorArea.Top;
        minMaxInfo.MaxSize.X = workArea.Right - workArea.Left;
        minMaxInfo.MaxSize.Y = workArea.Bottom - workArea.Top;
        ApplyMinimumTrackSize(hwnd, ref minMaxInfo);

        Marshal.StructureToPtr(minMaxInfo, lParam, true);
    }

    private static void ApplyMinimumTrackSize(IntPtr hwnd, ref MinMaxInfo minMaxInfo)
    {
        var source = HwndSource.FromHwnd(hwnd);

        if (source?.RootVisual is not Window window)
        {
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(window);

        if (window.MinWidth > 0d)
        {
            minMaxInfo.MinTrackSize.X = (int)Math.Ceiling(window.MinWidth * dpi.DpiScaleX);
        }

        if (window.MinHeight > 0d)
        {
            minMaxInfo.MinTrackSize.Y = (int)Math.Ceiling(window.MinHeight * dpi.DpiScaleY);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public Point Reserved;
        public Point MaxSize;
        public Point MaxPosition;
        public Point MinTrackSize;
        public Point MaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect MonitorArea;
        public Rect WorkArea;
        public int Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
