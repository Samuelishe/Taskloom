using System.Windows;
using System.Windows.Input;

namespace Taskloom.Presentation.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void CustomTitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
