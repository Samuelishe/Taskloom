namespace Taskloom.Presentation.ViewModels;

/// <summary>
/// Вариант языка интерфейса для окна настроек.
/// </summary>
public sealed class LanguageOptionViewModel
{
    public LanguageOptionViewModel(string cultureName, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cultureName);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        CultureName = cultureName;
        Title = title;
    }

    public string CultureName { get; }

    public string Title { get; }

    public override string ToString()
    {
        return Title;
    }
}
