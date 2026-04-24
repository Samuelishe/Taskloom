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
    private RecordListItemViewModel? _activeDragSourceRecord;
    private ScrollViewer? _recordsScrollViewer;
    private Guid[]? _dragPreviewOriginalOrderIds;
    private Guid? _previewTargetRecordId;
    private Dictionary<Guid, Rect>? _dragPreviewBoundsByRecordId;

    private const double DragAutoScrollEdgeThreshold = 56d;
    private const double DragAutoScrollStep = 24d;

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
            CapturePreviewOrderSnapshot();
            SetDragSourceState(dragRecord);
            DragDrop.DoDragDrop(
                RecordsList,
                new System.Windows.DataObject(typeof(RecordListItemViewModel), dragRecord),
                System.Windows.DragDropEffects.Move);
        }
        finally
        {
            RevertPreviewOrderIfNeeded();
            ClearDragSourceState();
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

        UpdateDragAutoScroll(e);
        var pointer = e.GetPosition(RecordsList);

        if (!TryResolveDropTarget(e, pointer, draggedRecord, out var targetRecord) ||
            targetRecord is null)
        {
            RevertPreviewOrderIfNeeded();
            e.Effects = System.Windows.DragDropEffects.None;
            ClearDropTargetState();
            e.Handled = true;
            return;
        }

        if (targetRecord.Id == draggedRecord.Id)
        {
            RevertPreviewOrderIfNeeded();
            e.Effects = System.Windows.DragDropEffects.None;
            ClearDropTargetState();
            e.Handled = true;
            return;
        }

        PreviewSwapOrder(draggedRecord, targetRecord);
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
                _activeDropTargetRecord is null ||
                _activeDropTargetRecord.Id == draggedRecord.Id)
            {
                RevertPreviewOrderIfNeeded();
                return;
            }

            await viewModel.ReorderRecordAsync(draggedRecord, _activeDropTargetRecord);
            _dragPreviewOriginalOrderIds = null;
            _previewTargetRecordId = null;
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
        ResetDragAutoScroll();

        if (e.OriginalSource is not DependencyObject source || FindAncestor<System.Windows.Controls.ListBox>(source) is null)
        {
            RevertPreviewOrderIfNeeded();
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

            source = GetParentObject(source);
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
        System.Windows.DragEventArgs e,
        System.Windows.Point pointer,
        RecordListItemViewModel draggedRecord,
        out RecordListItemViewModel? targetRecord)
    {
        targetRecord = null;

        if (_dragPreviewBoundsByRecordId is not null)
        {
            return TryResolveDropTargetFromSnapshot(pointer, draggedRecord, out targetRecord);
        }

        var source = e.OriginalSource as DependencyObject;
        var directContainer = FindAncestor<ListBoxItem>(source);

        if (directContainer?.DataContext is RecordListItemViewModel directRecord &&
            directRecord.Id != draggedRecord.Id)
        {
            targetRecord = directRecord;
            return true;
        }

        return TryResolveDropTargetFromCurrentLayout(pointer, draggedRecord, out targetRecord);
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

    private void SetDragSourceState(RecordListItemViewModel dragRecord)
    {
        if (_activeDragSourceRecord == dragRecord)
        {
            return;
        }

        ClearDragSourceState();
        _activeDragSourceRecord = dragRecord;
        _activeDragSourceRecord.IsDragSource = true;
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

    private void ClearDragSourceState()
    {
        if (_activeDragSourceRecord is null)
        {
            return;
        }

        _activeDragSourceRecord.IsDragSource = false;
        _activeDragSourceRecord = null;
    }

    private void UpdateDragAutoScroll(System.Windows.DragEventArgs e)
    {
        var scrollViewer = GetRecordsScrollViewer();

        if (scrollViewer is null)
        {
            return;
        }

        var position = e.GetPosition(scrollViewer);
        var offset = scrollViewer.VerticalOffset;

        if (position.Y <= DragAutoScrollEdgeThreshold)
        {
            scrollViewer.ScrollToVerticalOffset(Math.Max(0, offset - DragAutoScrollStep));
            return;
        }

        if (position.Y >= scrollViewer.ViewportHeight - DragAutoScrollEdgeThreshold)
        {
            scrollViewer.ScrollToVerticalOffset(Math.Min(scrollViewer.ScrollableHeight, offset + DragAutoScrollStep));
        }
    }

    private void ResetDragAutoScroll()
    {
        _recordsScrollViewer ??= GetRecordsScrollViewer();
    }

    private ScrollViewer? GetRecordsScrollViewer()
    {
        _recordsScrollViewer ??= FindDescendant<ScrollViewer>(RecordsList);
        return _recordsScrollViewer;
    }

    private void CapturePreviewOrderSnapshot()
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        _dragPreviewOriginalOrderIds = viewModel.Records
            .Select(item => item.Id)
            .ToArray();
        _previewTargetRecordId = null;
        _dragPreviewBoundsByRecordId = CaptureDragPreviewBounds(viewModel.Records);
    }

    private void PreviewSwapOrder(
        RecordListItemViewModel draggedRecord,
        RecordListItemViewModel targetRecord)
    {
        if (DataContext is not MainWindowViewModel viewModel ||
            _dragPreviewOriginalOrderIds is null)
        {
            return;
        }

        if (_previewTargetRecordId == targetRecord.Id)
        {
            return;
        }

        var desiredOrderIds = (Guid[])_dragPreviewOriginalOrderIds.Clone();
        var draggedIndex = Array.IndexOf(desiredOrderIds, draggedRecord.Id);
        var targetIndex = Array.IndexOf(desiredOrderIds, targetRecord.Id);

        if (draggedIndex < 0 || targetIndex < 0 || draggedIndex == targetIndex)
        {
            return;
        }

        (desiredOrderIds[draggedIndex], desiredOrderIds[targetIndex]) =
            (desiredOrderIds[targetIndex], desiredOrderIds[draggedIndex]);

        viewModel.ApplyPreviewOrder(desiredOrderIds);
        _previewTargetRecordId = targetRecord.Id;
    }

    private void RevertPreviewOrderIfNeeded()
    {
        if (DataContext is not MainWindowViewModel viewModel ||
            _dragPreviewOriginalOrderIds is null)
        {
            return;
        }

        viewModel.RestorePreviewOrder(_dragPreviewOriginalOrderIds);
        _previewTargetRecordId = null;
    }

    private bool TryResolveDropTargetFromSnapshot(
        System.Windows.Point pointer,
        RecordListItemViewModel draggedRecord,
        out RecordListItemViewModel? targetRecord)
    {
        targetRecord = null;

        if (_dragPreviewBoundsByRecordId is null ||
            DataContext is not MainWindowViewModel viewModel)
        {
            return false;
        }

        var pointerInContent = ToContentPoint(pointer);
        var bestRecord = default(RecordListItemViewModel);
        var bestDistance = double.MaxValue;
        var isDraggedBoundsNearest = false;

        if (_dragPreviewBoundsByRecordId.TryGetValue(draggedRecord.Id, out var draggedBounds))
        {
            if (draggedBounds.Contains(pointerInContent))
            {
                return false;
            }

            bestDistance = GetDistanceSquared(pointerInContent, draggedBounds);
            isDraggedBoundsNearest = true;
        }

        foreach (var record in viewModel.Records)
        {
            if (record.Id == draggedRecord.Id ||
                !_dragPreviewBoundsByRecordId.TryGetValue(record.Id, out var rect))
            {
                continue;
            }

            if (rect.Contains(pointerInContent))
            {
                targetRecord = record;
                return true;
            }

            var distance = GetDistanceSquared(pointerInContent, rect);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestRecord = record;
                isDraggedBoundsNearest = false;
            }
        }

        if (bestRecord is null || isDraggedBoundsNearest)
        {
            if (_previewTargetRecordId is not null)
            {
                RevertPreviewOrderIfNeeded();
                ClearDropTargetState();
            }

            return false;
        }

        targetRecord = bestRecord;
        return true;
    }

    private bool TryResolveDropTargetFromCurrentLayout(
        System.Windows.Point pointer,
        RecordListItemViewModel draggedRecord,
        out RecordListItemViewModel? targetRecord)
    {
        targetRecord = null;
        var bestRecord = default(RecordListItemViewModel);
        var bestDistance = double.MaxValue;

        foreach (var item in RecordsList.Items)
        {
            if (item is not RecordListItemViewModel record || record.Id == draggedRecord.Id)
            {
                continue;
            }

            if (RecordsList.ItemContainerGenerator.ContainerFromItem(item) is not ListBoxItem container ||
                container.ActualWidth <= 0 ||
                container.ActualHeight <= 0)
            {
                continue;
            }

            var rect = GetBounds(container);

            if (rect.Contains(pointer))
            {
                targetRecord = record;
                return true;
            }

            var distance = GetDistanceSquared(pointer, rect);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestRecord = record;
            }
        }

        if (bestRecord is null)
        {
            return false;
        }

        targetRecord = bestRecord;
        return true;
    }

    private Dictionary<Guid, Rect> CaptureDragPreviewBounds(IEnumerable<RecordListItemViewModel> records)
    {
        var result = new Dictionary<Guid, Rect>();

        foreach (var record in records)
        {
            if (!TryGetRecordBounds(record, out var bounds))
            {
                continue;
            }

            result[record.Id] = bounds;
        }

        return result;
    }

    private bool TryGetRecordBounds(RecordListItemViewModel record, out Rect bounds)
    {
        bounds = default;

        if (RecordsList.ItemContainerGenerator.ContainerFromItem(record) is not ListBoxItem container ||
            container.ActualWidth <= 0 ||
            container.ActualHeight <= 0)
        {
            return false;
        }

        bounds = GetBounds(container);
        bounds.Y += GetRecordsVerticalOffset();
        return true;
    }

    private Rect GetBounds(ListBoxItem container)
    {
        var topLeft = container.TranslatePoint(new System.Windows.Point(0, 0), RecordsList);
        return new Rect(topLeft.X, topLeft.Y, container.ActualWidth, container.ActualHeight);
    }

    private System.Windows.Point ToContentPoint(System.Windows.Point pointer)
    {
        return new System.Windows.Point(pointer.X, pointer.Y + GetRecordsVerticalOffset());
    }

    private double GetRecordsVerticalOffset()
    {
        return GetRecordsScrollViewer()?.VerticalOffset ?? 0d;
    }

    private static double GetDistanceSquared(System.Windows.Point point, Rect rect)
    {
        var dx = point.X < rect.Left
            ? rect.Left - point.X
            : point.X > rect.Right
                ? point.X - rect.Right
                : 0;
        var dy = point.Y < rect.Top
            ? rect.Top - point.Y
            : point.Y > rect.Bottom
                ? point.Y - rect.Bottom
                : 0;
        return dx * dx + dy * dy;
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

    private static T? FindDescendant<T>(DependencyObject? source)
        where T : DependencyObject
    {
        if (source is null)
        {
            return null;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(source); index++)
        {
            var child = VisualTreeHelper.GetChild(source, index);

            if (child is T target)
            {
                return target;
            }

            var nested = FindDescendant<T>(child);

            if (nested is not null)
            {
                return nested;
            }
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
