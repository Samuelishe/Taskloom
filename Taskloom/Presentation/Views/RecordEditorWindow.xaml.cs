using System.Windows;
using System.Windows.Input;
using System.IO;
using Taskloom.Common.Windowing;
using Taskloom.Presentation.ViewModels;
using Taskloom.Services.Records;
using Taskloom.Infrastructure.Storage;

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

    private void BrowseImagesButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not RecordEditorViewModel viewModel)
        {
            return;
        }

        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = App.CurrentApp.LocalizationService.GetString("Editor.ImageBrowseDialogTitle"),
            Filter = App.CurrentApp.LocalizationService.GetString("Editor.ImageFileDialogFilter"),
            CheckFileExists = true,
            Multiselect = true
        };

        if (openFileDialog.ShowDialog(this) == true)
        {
            viewModel.AttachImagesFromFiles(openFileDialog.FileNames);
        }
    }

    private async void BrowseAudiosButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not RecordEditorViewModel viewModel)
        {
            return;
        }

        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = App.CurrentApp.LocalizationService.GetString("Editor.AudioBrowseDialogTitle"),
            Filter = App.CurrentApp.LocalizationService.GetString("Editor.AudioFileDialogFilter"),
            CheckFileExists = true,
            Multiselect = true
        };

        if (openFileDialog.ShowDialog(this) == true)
        {
            try
            {
                await viewModel.AttachAudiosFromFilesAsync(openFileDialog.FileNames);
            }
            catch (Exception exception)
            {
                try
                {
                    var logPath = TaskloomPaths.GetAudioImportLogPath();
                    var directoryPath = Path.GetDirectoryName(logPath);
                    if (!string.IsNullOrWhiteSpace(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    File.AppendAllText(
                        logPath,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {exception}{Environment.NewLine}");
                }
                catch
                {
                    // Ошибка логирования не должна скрывать исходную ошибку импорта.
                }

                System.Windows.MessageBox.Show(
                    this,
                    $"{exception.Message}{Environment.NewLine}{Environment.NewLine}{TaskloomPaths.GetAudioImportLogPath()}",
                    App.CurrentApp.LocalizationService.GetString("App.Title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private void RemoveImageButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is RecordEditorViewModel viewModel &&
            sender is FrameworkElement { Tag: RecordImageDraft image })
        {
            viewModel.RemoveImage(image);
        }
    }

    private void MoveImageLeftButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is RecordEditorViewModel viewModel &&
            sender is FrameworkElement { Tag: RecordImageDraft image })
        {
            viewModel.MoveImageLeft(image);
        }
    }

    private void MoveImageRightButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is RecordEditorViewModel viewModel &&
            sender is FrameworkElement { Tag: RecordImageDraft image })
        {
            viewModel.MoveImageRight(image);
        }
    }

    private void RemoveAudioButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is RecordEditorViewModel viewModel &&
            sender is FrameworkElement { Tag: RecordAudioDraft audio })
        {
            viewModel.RemoveAudio(audio);
        }
    }

    private void MoveAudioLeftButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is RecordEditorViewModel viewModel &&
            sender is FrameworkElement { Tag: RecordAudioDraft audio })
        {
            viewModel.MoveAudioLeft(audio);
        }
    }

    private void MoveAudioRightButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is RecordEditorViewModel viewModel &&
            sender is FrameworkElement { Tag: RecordAudioDraft audio })
        {
            viewModel.MoveAudioRight(audio);
        }
    }

    private void ToggleWindowState()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}
