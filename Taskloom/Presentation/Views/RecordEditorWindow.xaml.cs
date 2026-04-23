using System.Windows;
using System.Windows.Input;
using Taskloom.Common.Windowing;

namespace Taskloom.Presentation.Views;

public partial class RecordEditorWindow : Window
{
    public RecordEditorWindow()
    {
        InitializeComponent();
        WindowMaximizeBoundsHelper.Attach(this);
        SourceInitialized += OnSourceInitialized;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        const double verticalMargin = 80;
        var availableHeight = Math.Max(MinHeight, SystemParameters.WorkArea.Height - verticalMargin);

        MaxHeight = availableHeight;
        Height = Math.Min(Height, availableHeight);
    }

    private void CustomTitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleWindowState();
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleWindowState()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}
