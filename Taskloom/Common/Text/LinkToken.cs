namespace Taskloom.Common.Text;

/// <summary>
/// Токен отображаемого текста описания записи.
/// </summary>
public sealed class LinkToken
{
    public LinkToken(string text, string? target = null)
    {
        Text = text;
        Target = target;
    }

    public string Text { get; }

    public string? Target { get; }

    public bool IsLink => !string.IsNullOrWhiteSpace(Target);
}
