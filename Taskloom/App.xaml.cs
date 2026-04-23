using System.IO;
using System.Windows;
using Taskloom.Infrastructure.Localization;
using Taskloom.Infrastructure.Notifications;
using Taskloom.Infrastructure.Repositories;
using Taskloom.Infrastructure.Settings;
using Taskloom.Infrastructure.Storage;
using Taskloom.Infrastructure.Theming;
using Taskloom.Infrastructure.Tray;
using Taskloom.Presentation.ViewModels;
using Taskloom.Presentation.Views;
using Taskloom.Services.Localization;
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
    private WindowsTrayService? _trayService;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
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
            var recordService = new CalendarRecordService(repository);
            _trayService = new WindowsTrayService(localizationService);
            _notificationService = CreateNotificationService(_trayService);
            _eventReminderService = new WindowsBalloonEventReminderService(recordService, localizationService, _notificationService);
            var mainWindowViewModel = new MainWindowViewModel(recordService, localizationService, SettingsService);

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
            _eventReminderService.Start();
        }
        catch (Exception exception)
        {
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
        _trayService?.Dispose();
        base.OnExit(e);
    }

    private static IAppNotificationService CreateNotificationService(WindowsTrayService trayService)
    {
        return new WindowsBalloonAppNotificationService(trayService.NotifyIcon);
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
