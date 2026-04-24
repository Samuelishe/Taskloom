using System.Windows;
using System.Windows.Input;
using Taskloom.Presentation.ViewModels;
using Taskloom.Services.Settings;

namespace Taskloom.Presentation.Views;

public partial class SettingsWindow : Window
{
    private bool _isCleanupSelectionInitializing = true;
    private RecordCleanupMode _lastConfirmedCleanupMode = RecordCleanupMode.Never;

    public SettingsWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
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

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            _lastConfirmedCleanupMode = viewModel.SelectedCleanupMode.CleanupMode;
        }

        _isCleanupSelectionInitializing = false;
    }

    private void CleanupModeComboBox_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isCleanupSelectionInitializing ||
            sender is not System.Windows.Controls.ComboBox comboBox ||
            DataContext is not SettingsViewModel viewModel ||
            comboBox.SelectedItem is not CleanupModeOptionViewModel selectedOption)
        {
            return;
        }

        if (!IsDangerousCleanupMode(selectedOption.CleanupMode))
        {
            _lastConfirmedCleanupMode = selectedOption.CleanupMode;
            return;
        }

        var result = System.Windows.MessageBox.Show(
            this,
            App.CurrentApp.LocalizationService.GetString("Settings.CleanupDangerousConfirmationMessage"),
            App.CurrentApp.LocalizationService.GetString("Settings.CleanupDangerousConfirmationTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            _lastConfirmedCleanupMode = selectedOption.CleanupMode;
            return;
        }

        _isCleanupSelectionInitializing = true;
        comboBox.SelectedItem = viewModel.GetCleanupModeOption(_lastConfirmedCleanupMode);
        _isCleanupSelectionInitializing = false;
    }

    private static bool IsDangerousCleanupMode(RecordCleanupMode cleanupMode)
    {
        return cleanupMode is RecordCleanupMode.DeleteAllOlderThan7Days or RecordCleanupMode.DeleteAllOlderThan1Month;
    }
}
