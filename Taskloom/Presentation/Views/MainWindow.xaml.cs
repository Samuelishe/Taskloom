using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Taskloom.Common.Windowing;
using Taskloom.Presentation.ViewModels;

namespace Taskloom.Presentation.Views;

public partial class MainWindow : Window
{
    private RecordEditorWindow? _editorWindow;
    private SettingsWindow? _settingsWindow;
    private bool _isWindowPlacementSaved;
    private bool _isApplicationExitRequested;
    private bool _isHidingToTray;

    public MainWindow()
    {
        InitializeComponent();
        WindowMaximizeBoundsHelper.Attach(this);
        DataContextChanged += OnDataContextChanged;
        SourceInitialized += OnSourceInitialized;
    }

    private async void OnSourceInitialized(object? sender, EventArgs e)
    {
        var settings = await App.CurrentApp.SettingsService.LoadAsync();

        if (!settings.HasMainWindowPlacement)
        {
            WindowState = WindowState.Maximized;
            return;
        }

        Width = Math.Max(MinWidth, settings.MainWindowWidth);
        Height = Math.Max(MinHeight, settings.MainWindowHeight);

        Left = (SystemParameters.WorkArea.Width - Width) / 2 + SystemParameters.WorkArea.Left;
        Top = (SystemParameters.WorkArea.Height - Height) / 2 + SystemParameters.WorkArea.Top;

        WindowState = string.Equals(settings.MainWindowState, nameof(System.Windows.WindowState.Maximized), StringComparison.Ordinal)
            ? WindowState.Maximized
            : WindowState.Normal;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainWindowViewModel oldViewModel)
        {
            oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (e.NewValue is MainWindowViewModel newViewModel)
        {
            newViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainWindowViewModel.ActiveEditor))
        {
            if (e.PropertyName != nameof(MainWindowViewModel.ActiveSettings))
            {
                return;
            }

            if (DataContext is not MainWindowViewModel settingsViewModel)
            {
                return;
            }

            if (settingsViewModel.ActiveSettings is null)
            {
                CloseSettingsWindow();
                return;
            }

            OpenSettingsWindow(settingsViewModel.ActiveSettings);
            return;
        }

        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.ActiveEditor is null)
        {
            CloseEditorWindow();
            return;
        }

        OpenEditorWindow(viewModel.ActiveEditor);
    }

    private async void RecordsList_OnPreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindInteractiveAudioElement(e.OriginalSource as DependencyObject) is not null)
        {
            e.Handled = true;
            return;
        }

        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.OpenEditRecordCommand.CanExecute(null))
        {
            await viewModel.OpenEditRecordCommand.ExecuteAsync(null);
        }
    }

    private async void DeleteRecordButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || viewModel.SelectedRecord is null)
        {
            return;
        }

        var result = System.Windows.MessageBox.Show(
            App.CurrentApp.LocalizationService.Format("MainWindow.DeleteConfirmationMessage", viewModel.SelectedRecord.Title),
            App.CurrentApp.LocalizationService.GetString("MainWindow.DeleteConfirmationTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (viewModel.DeleteRecordCommand.CanExecute(null))
        {
            await viewModel.DeleteRecordCommand.ExecuteAsync(null);
        }
    }

    private void AudioSeekSlider_OnDragCompleted(object sender, DragCompletedEventArgs e)
    {
        var slider = ResolveSlider(sender as DependencyObject);

        if (DataContext is not MainWindowViewModel viewModel ||
            slider is not { Tag: RecordAudioListItemViewModel audioItem })
        {
            return;
        }

        viewModel.EndSeekAudio(audioItem);
        viewModel.SeekAudio(audioItem, slider.Value);
        e.Handled = true;
    }

    private void AudioSeekSlider_OnDragStarted(object sender, DragStartedEventArgs e)
    {
        var slider = ResolveSlider(sender as DependencyObject);

        if (DataContext is not MainWindowViewModel viewModel ||
            slider is not { Tag: RecordAudioListItemViewModel audioItem })
        {
            return;
        }

        viewModel.BeginSeekAudio(audioItem);
        e.Handled = true;
    }

    private void AudioSeekSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DataContext is not MainWindowViewModel viewModel ||
            sender is not Slider { Tag: RecordAudioListItemViewModel audioItem, IsMouseCaptureWithin: true } slider)
        {
            return;
        }

        viewModel.UpdateSeekAudioPreview(audioItem, slider.Value);
    }

    private void AudioSeekSlider_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel ||
            sender is not Slider { Tag: RecordAudioListItemViewModel audioItem } slider)
        {
            return;
        }

        viewModel.EndSeekAudio(audioItem);
        viewModel.SeekAudio(audioItem, slider.Value);
        e.Handled = true;
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

    private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeRestoreButton_OnClick(object sender, RoutedEventArgs e)
    {
        ToggleWindowState();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    public void RestoreFromTray()
    {
        Show();

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    public void RequestApplicationExit()
    {
        _isApplicationExitRequested = true;
        Close();
    }

    private void ToggleWindowState()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void OpenEditorWindow(RecordEditorViewModel editorViewModel)
    {
        if (_editorWindow is not null)
        {
            if (_editorWindow.DataContext is RecordEditorViewModel oldEditorViewModel)
            {
                oldEditorViewModel.CloseRequested -= OnEditorCloseRequested;
            }

            _editorWindow.DataContext = editorViewModel;
            editorViewModel.CloseRequested += OnEditorCloseRequested;
            _editorWindow.Activate();
            return;
        }

        _editorWindow = new RecordEditorWindow
        {
            Owner = this,
            DataContext = editorViewModel
        };

        editorViewModel.CloseRequested += OnEditorCloseRequested;
        _editorWindow.Closed += OnEditorWindowClosed;
        _editorWindow.Show();
    }

    private void OnEditorCloseRequested(object? sender, RecordEditorCloseRequestedEventArgs e)
    {
        CloseEditorWindow();
    }

    private void OnEditorWindowClosed(object? sender, EventArgs e)
    {
        if (_editorWindow?.DataContext is RecordEditorViewModel editorViewModel)
        {
            editorViewModel.CloseRequested -= OnEditorCloseRequested;
        }

        if (DataContext is MainWindowViewModel mainWindowViewModel)
        {
            mainWindowViewModel.ActiveEditor = null;
        }

        if (_editorWindow is not null)
        {
            _editorWindow.Closed -= OnEditorWindowClosed;
        }

        _editorWindow = null;
    }

    private void CloseEditorWindow()
    {
        if (_editorWindow is null)
        {
            return;
        }

        var editorWindow = _editorWindow;
        _editorWindow = null;
        editorWindow.Close();
    }

    private void OpenSettingsWindow(SettingsViewModel settingsViewModel)
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow
        {
            Owner = this,
            DataContext = settingsViewModel
        };

        settingsViewModel.CloseRequested += OnSettingsCloseRequested;
        _settingsWindow.Closed += OnSettingsWindowClosed;
        _settingsWindow.Show();
    }

    private void OnSettingsCloseRequested(object? sender, SettingsCloseRequestedEventArgs e)
    {
        CloseSettingsWindow();
    }

    private void OnSettingsWindowClosed(object? sender, EventArgs e)
    {
        if (_settingsWindow?.DataContext is SettingsViewModel settingsViewModel)
        {
            settingsViewModel.CloseRequested -= OnSettingsCloseRequested;
        }

        if (DataContext is MainWindowViewModel mainWindowViewModel)
        {
            mainWindowViewModel.CloseSettings();
        }

        if (_settingsWindow is not null)
        {
            _settingsWindow.Closed -= OnSettingsWindowClosed;
        }

        _settingsWindow = null;
    }

    private void CloseSettingsWindow()
    {
        if (_settingsWindow is null)
        {
            return;
        }

        var settingsWindow = _settingsWindow;
        _settingsWindow = null;
        settingsWindow.Close();
    }

    protected override async void OnClosing(CancelEventArgs e)
    {
        if (!_isApplicationExitRequested)
        {
            e.Cancel = true;

            if (_isHidingToTray)
            {
                return;
            }

            _isHidingToTray = true;

            try
            {
                CloseEditorWindow();
                CloseSettingsWindow();
                await SaveWindowPlacementAsync();
                Hide();
            }
            finally
            {
                _isHidingToTray = false;
            }

            return;
        }

        if (!_isWindowPlacementSaved)
        {
            e.Cancel = true;
            CloseEditorWindow();
            CloseSettingsWindow();
            await SaveWindowPlacementAsync();
            _isWindowPlacementSaved = true;
            Close();
            return;
        }

        CloseEditorWindow();
        CloseSettingsWindow();
        base.OnClosing(e);
    }

    private async Task SaveWindowPlacementAsync()
    {
        var settings = await App.CurrentApp.SettingsService.LoadAsync();
        var bounds = RestoreBounds;

        settings.HasMainWindowPlacement = true;
        settings.MainWindowWidth = Math.Max(MinWidth, bounds.Width);
        settings.MainWindowHeight = Math.Max(MinHeight, bounds.Height);
        settings.MainWindowState = WindowState == WindowState.Maximized
            ? nameof(System.Windows.WindowState.Maximized)
            : nameof(System.Windows.WindowState.Normal);

        await App.CurrentApp.SettingsService.SaveAsync(settings);
    }

    private static DependencyObject? FindInteractiveAudioElement(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is System.Windows.Controls.Button or Slider or Thumb)
            {
                return source;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }

    private static Slider? ResolveSlider(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is Slider slider)
            {
                return slider;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }
}
