using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace Taskloom.Common.Text;

/// <summary>
/// Рендерит кликабельные ссылки внутри обычного TextBlock.
/// </summary>
public static class TextBlockLinkBehavior
{
    public static readonly DependencyProperty LinkTextProperty = DependencyProperty.RegisterAttached(
        "LinkText",
        typeof(string),
        typeof(TextBlockLinkBehavior),
        new PropertyMetadata(string.Empty, OnLinkTextChanged));

    public static string GetLinkText(DependencyObject element)
    {
        return (string)element.GetValue(LinkTextProperty);
    }

    public static void SetLinkText(DependencyObject element, string value)
    {
        element.SetValue(LinkTextProperty, value);
    }

    private static void OnLinkTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not TextBlock textBlock)
        {
            return;
        }

        textBlock.Inlines.Clear();

        foreach (var token in LinkParser.Parse(args.NewValue?.ToString()))
        {
            if (!token.IsLink)
            {
                textBlock.Inlines.Add(new Run(token.Text));
                continue;
            }

            var hyperlink = new Hyperlink(new Run(token.Text))
            {
                NavigateUri = new Uri(token.Target!, UriKind.Absolute)
            };

            hyperlink.RequestNavigate += OnHyperlinkRequestNavigate;
            textBlock.Inlines.Add(hyperlink);
        }
    }

    private static void OnHyperlinkRequestNavigate(object sender, RequestNavigateEventArgs eventArgs)
    {
        try
        {
            Process.Start(new ProcessStartInfo(eventArgs.Uri.AbsoluteUri)
            {
                UseShellExecute = true
            });
        }
        catch
        {
            // Ошибка открытия ссылки не должна ломать UI.
        }

        eventArgs.Handled = true;
    }
}
