namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// Аргументы закрытия окна редактора записи.
/// </summary>
public sealed class RecordEditorCloseRequestedEventArgs : EventArgs
{
    public RecordEditorCloseRequestedEventArgs(bool isSaved)
    {
        IsSaved = isSaved;
    }

    public bool IsSaved { get; }
}
