namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// Аргументы закрытия окна настроек.
/// </summary>
public sealed class SettingsCloseRequestedEventArgs : EventArgs
{
    public SettingsCloseRequestedEventArgs(bool isSaved)
    {
        IsSaved = isSaved;
    }

    public bool IsSaved { get; }
}
