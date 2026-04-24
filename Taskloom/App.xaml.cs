using System.IO;
using System.Windows;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using Taskloom.Infrastructure.Localization;
using Taskloom.Infrastructure.Links;
using Taskloom.Infrastructure.Media;
using Taskloom.Infrastructure.Notifications;
using Taskloom.Infrastructure.Repositories;
using Taskloom.Infrastructure.Settings;
using Taskloom.Infrastructure.Storage;
using Taskloom.Infrastructure.Theming;
using Taskloom.Infrastructure.Tray;
using Taskloom.Presentation.ViewModels;
using Taskloom.Presentation.Views;
using Taskloom.Services.Localization;
using Taskloom.Services.Links;
using Taskloom.Services.Media;
using Taskloom.Services.Notifications;
using Taskloom.Services.Records;
using Taskloom.Services.Settings;
using Taskloom.Services.Theming;

namespace Taskloom;

public partial class App : System.Windows.Application
{
    public static App CurrentApp => (App)Current;

    public ILocalizationService LocalizationService { get; private set; } = null!;

    public IAppSettingsService SettingsService { get; private set; } = null!;

    public IThemeService ThemeService { get; private set; } = null!;

    private IEventReminderService? _eventReminderService;
    private IAppNotificationService? _notificationService;
    private IAudioPlaybackService? _audioPlaybackService;
    private WindowsTrayService? _trayService;
    private bool _windowsAppSdkBootstrapped;
    private readonly string _notificationLogPath = TaskloomPaths.GetNotificationLogPath();
    private readonly string _startupLogPath = TaskloomPaths.GetStartupLogPath();
    private readonly string _recordCleanupLogPath = TaskloomPaths.GetRecordCleanupLogPath();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        RegisterGlobalExceptionHandlers();

        try
        {
            InitializeWindowsAppSdkBootstrap();

            SettingsService = new AppSettingsService(TaskloomPaths.GetSettingsPath());
            var settings = await SettingsService.LoadAsync();

            ThemeService = new ThemeService(this);
            ThemeService.ApplyTheme(settings.ThemeId);

            var localizationDirectoryPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Localization");
            var localizationService = new LocalizationService(localizationDirectoryPath);
            await localizationService.SetCultureAsync(settings.LanguageCultureName);

            LocalizationService = localizationService;
            LocalizationManager.Initialize(localizationService.Source);

            var databasePath = TaskloomPaths.GetDatabasePath();
            var connectionFactory = new SqliteConnectionFactory(databasePath);
            var databaseInitializer = new SqliteDatabaseInitializer(connectionFactory);
            await databaseInitializer.InitializeAsync();

            var repository = new SqliteCalendarRecordRepository(connectionFactory);
            var imageStorageService = new RecordImageStorageService();
            var audioStorageService = new RecordAudioStorageService();
            var recordResourceMetadataService = new RecordResourceMetadataService();
            ILinkPreviewService linkPreviewService = new LinkPreviewService();
            _audioPlaybackService = new AudioPlaybackService();
            var recordService = new CalendarRecordService(repository, imageStorageService, audioStorageService, recordResourceMetadataService);
            await RunStartupCleanupAsync(recordService, settings.RecordCleanupMode);
            _trayService = new WindowsTrayService(localizationService);
            _notificationService = CreateNotificationService(localizationService, _trayService);
            _eventReminderService = new WindowsBalloonEventReminderService(recordService, localizationService, _notificationService);
            var mainWindowViewModel = new MainWindowViewModel(
                recordService,
                localizationService,
                SettingsService,
                imageStorageService,
                audioStorageService,
                _audioPlaybackService,
                linkPreviewService);

            await mainWindowViewModel.InitializeAsync();

            var mainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };

            MainWindow = mainWindow;
            _trayService.OpenRequested += OnTrayOpenRequested;
            _trayService.SettingsRequested += OnTraySettingsRequested;
            _trayService.ExitRequested += OnTrayExitRequested;
            mainWindow.Show();
            mainWindow.Activate();
            _eventReminderService.Start();
        }
        catch (Exception exception)
        {
            TaskloomDiagnosticLog.AppendException(_startupLogPath, exception, "Startup failure");

            var title = LocalizationService?.GetString("Startup.ErrorTitle") ?? "Taskloom";
            var message = LocalizationService is null
                ? $"Не удалось запустить приложение.{Environment.NewLine}{Environment.NewLine}{exception.Message}"
                : LocalizationService.Format("Startup.ErrorMessage", exception.Message);

            System.Windows.MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _eventReminderService?.Dispose();
        _notificationService?.Dispose();
        _audioPlaybackService?.Dispose();
        _trayService?.Dispose();

        if (_windowsAppSdkBootstrapped)
        {
            try
            {
                Bootstrap.Shutdown();
            }
            catch
            {
                // Ошибка завершения bootstrapper не должна ломать выход из приложения.
            }
        }

        base.OnExit(e);
    }

    private IAppNotificationService CreateNotificationService(
        LocalizationService localizationService,
        WindowsTrayService trayService)
    {
        var fallbackNotificationService = new WindowsBalloonAppNotificationService(trayService.NotifyIcon);

        return new WindowsAppSdkNotificationService(
            fallbackNotificationService,
            () => Dispatcher.BeginInvoke(OnNotificationActivated));
    }

    private void OnNotificationActivated()
    {
        OnTrayOpenRequested(this, EventArgs.Empty);
    }

    private async Task RunStartupCleanupAsync(ICalendarRecordService recordService, RecordCleanupMode cleanupMode)
    {
        try
        {
            var deletedRecordsCount = await recordService.CleanupOldRecordsAsync(cleanupMode);

            if (deletedRecordsCount > 0)
            {
                TaskloomDiagnosticLog.Append(
                    _recordCleanupLogPath,
                    $"Startup cleanup completed. Mode={cleanupMode}. DeletedRecords={deletedRecordsCount}.");
            }
        }
        catch (Exception exception)
        {
            TaskloomDiagnosticLog.AppendException(
                _recordCleanupLogPath,
                exception,
                $"Startup cleanup failed. Mode={cleanupMode}");
        }
    }

    private void InitializeWindowsAppSdkBootstrap()
    {
        try
        {
            var initializeResult = Bootstrap.TryInitialize(0x00010008, out var hresult);
            _windowsAppSdkBootstrapped = initializeResult;

            if (initializeResult)
            {
                WriteNotificationDiagnostic("Windows App SDK bootstrap initialized successfully.");
                return;
            }

            WriteNotificationDiagnostic($"Windows App SDK bootstrap failed. HRESULT=0x{hresult:X8}.");
        }
        catch (Exception exception)
        {
            _windowsAppSdkBootstrapped = false;
            WriteNotificationDiagnostic($"Windows App SDK bootstrap threw exception. {exception}");
        }
    }

    private void WriteNotificationDiagnostic(string message)
    {
        TaskloomDiagnosticLog.Append(_notificationLogPath, message);
    }

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnCurrentDomainUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        TaskloomDiagnosticLog.AppendException(_startupLogPath, e.Exception, "Dispatcher unhandled exception");
    }

    private void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            TaskloomDiagnosticLog.AppendException(_startupLogPath, exception, "AppDomain unhandled exception");
            return;
        }

        TaskloomDiagnosticLog.Append(_startupLogPath, $"AppDomain unhandled exception object: {e.ExceptionObject}");
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        TaskloomDiagnosticLog.AppendException(_startupLogPath, e.Exception, "Unobserved task exception");
        e.SetObserved();
    }

    private void OnTrayOpenRequested(object? sender, EventArgs e)
    {
        if (MainWindow is MainWindow mainWindow)
        {
            mainWindow.RestoreFromTray();
        }
    }

    private void OnTraySettingsRequested(object? sender, EventArgs e)
    {
        if (MainWindow is not MainWindow mainWindow)
        {
            return;
        }

        mainWindow.RestoreFromTray();

        if (mainWindow.DataContext is MainWindowViewModel viewModel &&
            viewModel.OpenSettingsCommand.CanExecute(null))
        {
            viewModel.OpenSettingsCommand.Execute(null);
        }
    }

    private void OnTrayExitRequested(object? sender, EventArgs e)
    {
        if (MainWindow is MainWindow mainWindow)
        {
            mainWindow.RequestApplicationExit();
            return;
        }

        Shutdown();
    }
}
