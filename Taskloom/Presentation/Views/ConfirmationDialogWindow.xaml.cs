using System.Windows;
using System.Windows.Input;

namespace Taskloom.Presentation.Views;

public partial class ConfirmationDialogWindow : Window
{
    public ConfirmationDialogWindow(
        string title,
        string message,
        string confirmButtonText,
        string cancelButtonText)
    {
        InitializeComponent();

        Title = title;
        TitleTextBlock.Text = title;
        MessageTextBlock.Text = message;
        ConfirmButton.Content = confirmButtonText;
        CancelButton.Content = cancelButtonText;
    }

    public static bool ShowYesNo(
        Window? owner,
        string title,
        string message,
        string confirmButtonText,
        string cancelButtonText)
    {
        var dialog = new ConfirmationDialogWindow(title, message, confirmButtonText, cancelButtonText);

        if (owner is not null)
        {
            dialog.Owner = owner;
        }

        var result = dialog.ShowDialog();
        return result == true;
    }

    private void CustomTitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
