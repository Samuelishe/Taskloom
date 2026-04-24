using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Taskloom.Common.Windowing;
using Taskloom.Infrastructure.Storage;
using Taskloom.Presentation.ViewModels;

namespace Taskloom.Presentation.Views;

public partial class MainWindow : Window
{
    private RecordEditorWindow? _editorWindow;
    private SettingsWindow? _settingsWindow;
    private bool _isWindowPlacementSaved;
    private bool _isApplicationExitRequested;
    private bool _isHidingToTray;
    private System.Windows.Point? _recordDragStartPoint;
    private RecordListItemViewModel? _dragCandidateRecord;
    private RecordListItemViewModel? _activeDropTargetRecord;

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

        var localizationService = App.CurrentApp.LocalizationService;
        var isConfirmed = ConfirmationDialogWindow.ShowYesNo(
            this,
            localizationService.GetString("MainWindow.DeleteConfirmationTitle"),
            localizationService.Format("MainWindow.DeleteConfirmationMessage", viewModel.SelectedRecord.Title),
            localizationService.GetString("Dialog.Yes"),
            localizationService.GetString("Dialog.No"));

        if (!isConfirmed)
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

    private void RecordsList_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _recordDragStartPoint = e.GetPosition(RecordsList);
        _dragCandidateRecord = TryGetRecordFromSource(e.OriginalSource as DependencyObject);
    }

    private void RecordsList_OnPreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed ||
            _recordDragStartPoint is null ||
            _dragCandidateRecord is null)
        {
            return;
        }

        var currentPoint = e.GetPosition(RecordsList);
        var offset = currentPoint - _recordDragStartPoint.Value;

        if (Math.Abs(offset.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(offset.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var source = e.OriginalSource as DependencyObject;

        if (source is null ||
            FindSelectionPreservingElement(source) is not null ||
            FindInteractiveAudioElement(source) is not null ||
            FindAncestor<TextElement>(source) is not null)
        {
            return;
        }

        var dragRecord = _dragCandidateRecord;
        _recordDragStartPoint = null;
        _dragCandidateRecord = null;

        try
        {
            DragDrop.DoDragDrop(
                RecordsList,
                new System.Windows.DataObject(typeof(RecordListItemViewModel), dragRecord),
                System.Windows.DragDropEffects.Move);
        }
        finally
        {
            ClearDropTargetState();
        }
    }

    private void RecordsList_OnDragOver(object sender, System.Windows.DragEventArgs e)
    {
        var draggedRecord = TryGetDraggedRecord(e);

        if (draggedRecord is null)
        {
            e.Effects = System.Windows.DragDropEffects.None;
            ClearDropTargetState();
            e.Handled = true;
            return;
        }

        if (!TryResolveDropTarget(e.OriginalSource as DependencyObject, out var targetRecord) ||
            targetRecord is null ||
            targetRecord.Id == draggedRecord.Id)
        {
            e.Effects = System.Windows.DragDropEffects.None;
            ClearDropTargetState();
            e.Handled = true;
            return;
        }

        SetDropTargetState(targetRecord);
        e.Effects = System.Windows.DragDropEffects.Move;
        e.Handled = true;
    }

    private async void RecordsList_OnDrop(object sender, System.Windows.DragEventArgs e)
    {
        try
        {
            var draggedRecord = TryGetDraggedRecord(e);

            if (draggedRecord is null ||
                DataContext is not MainWindowViewModel viewModel ||
                !TryResolveDropTarget(e.OriginalSource as DependencyObject, out var targetRecord) ||
                targetRecord is null ||
                draggedRecord.Id == targetRecord.Id)
            {
                return;
            }

            await viewModel.ReorderRecordAsync(draggedRecord, targetRecord);
        }
        finally
        {
            ClearDropTargetState();
            _recordDragStartPoint = null;
            _dragCandidateRecord = null;
        }
    }

    private void RecordsList_OnDragLeave(object sender, System.Windows.DragEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source || FindAncestor<System.Windows.Controls.ListBox>(source) is null)
        {
            ClearDropTargetState();
        }
    }

    private void MainWindow_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || viewModel.SelectedRecord is null)
        {
            return;
        }

        var source = e.OriginalSource as DependencyObject;

        if (source is null)
        {
            return;
        }

        if (FindAncestor<ListBoxItem>(source) is not null)
        {
            return;
        }

        if (FindSelectionPreservingElement(source) is not null)
        {
            return;
        }

        viewModel.SelectedRecord = null;
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
        try
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
        catch (Exception exception)
        {
            TaskloomDiagnosticLog.AppendException(
                TaskloomPaths.GetStartupLogPath(),
                exception,
                "Failed to save main window placement");
        }
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

    private RecordListItemViewModel? TryGetRecordFromSource(DependencyObject? source)
    {
        var container = FindAncestor<ListBoxItem>(source);
        return container?.DataContext as RecordListItemViewModel;
    }

    private static RecordListItemViewModel? TryGetDraggedRecord(System.Windows.DragEventArgs e)
    {
        return e.Data.GetData(typeof(RecordListItemViewModel)) as RecordListItemViewModel;
    }

    private bool TryResolveDropTarget(
        DependencyObject? source,
        out RecordListItemViewModel? targetRecord)
    {
        targetRecord = null;

        var container = FindAncestor<ListBoxItem>(source);

        if (container?.DataContext is RecordListItemViewModel record)
        {
            targetRecord = record;
            return true;
        }

        return false;
    }

    private void SetDropTargetState(RecordListItemViewModel targetRecord)
    {
        if (_activeDropTargetRecord == targetRecord)
        {
            return;
        }

        ClearDropTargetState();
        _activeDropTargetRecord = targetRecord;
        _activeDropTargetRecord.IsDropTarget = true;
    }

    private void ClearDropTargetState()
    {
        if (_activeDropTargetRecord is null)
        {
            return;
        }

        _activeDropTargetRecord.IsDropTarget = false;
        _activeDropTargetRecord = null;
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

    private static DependencyObject? FindSelectionPreservingElement(DependencyObject? source)
    {
        return FindAncestor<System.Windows.Controls.Primitives.ButtonBase>(source) as DependencyObject
               ?? FindAncestor<System.Windows.Controls.Primitives.TextBoxBase>(source) as DependencyObject
               ?? FindAncestor<PasswordBox>(source)
               ?? FindAncestor<Slider>(source)
               ?? FindAncestor<System.Windows.Controls.Primitives.ScrollBar>(source) as DependencyObject
               ?? FindAncestor<Thumb>(source);
    }

    private static T? FindAncestor<T>(DependencyObject? source)
        where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T target)
            {
                return target;
            }

            source = GetParentObject(source);
        }

        return null;
    }

    private static DependencyObject? GetParentObject(DependencyObject source)
    {
        if (source is Visual || source is Visual3D)
        {
            return VisualTreeHelper.GetParent(source);
        }

        if (source is FrameworkContentElement frameworkContentElement)
        {
            return frameworkContentElement.Parent;
        }

        if (source is ContentElement contentElement)
        {
            return ContentOperations.GetParent(contentElement);
        }

        return null;
    }
}
