using System.Diagnostics;

namespace Taskloom.Common.Text;

/// <summary>
/// Открывает внешние ссылки через системный обработчик.
/// </summary>
public static class LinkNavigator
{
    public static bool TryOpen(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo(target)
            {
                UseShellExecute = true
            });

            return true;
        }
        catch
        {
            return false;
        }
    }
}
