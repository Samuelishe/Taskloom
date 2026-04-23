namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// Элемент выбора части времени.
/// </summary>
public sealed class TimePartOptionViewModel
{
    public TimePartOptionViewModel(int value)
    {
        Value = value;
        Title = value.ToString("00");
    }

    public int Value { get; }

    public string Title { get; }

    public override string ToString()
    {
        return Title;
    }
}
