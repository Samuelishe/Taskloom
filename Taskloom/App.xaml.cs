using System.IO;
using System.Windows;
using Taskloom.Infrastructure.Localization;
using Taskloom.Infrastructure.Repositories;
using Taskloom.Infrastructure.Settings;
using Taskloom.Infrastructure.Storage;
using Taskloom.Presentation.ViewModels;
using Taskloom.Presentation.Views;
using Taskloom.Services.Localization;
using Taskloom.Services.Records;
using Taskloom.Services.Settings;

namespace Taskloom;

public partial class App : Application
{
    public static App CurrentApp => (App)Current;

    public ILocalizationService LocalizationService { get; private set; } = null!;

    public IAppSettingsService SettingsService { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            SettingsService = new AppSettingsService(TaskloomPaths.GetSettingsPath());
            var settings = await SettingsService.LoadAsync();

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
            var mainWindowViewModel = new MainWindowViewModel(recordService, localizationService, SettingsService);

            await mainWindowViewModel.InitializeAsync();

            var mainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };

            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception exception)
        {
            var title = LocalizationService?.GetString("Startup.ErrorTitle") ?? "Taskloom";
            var message = LocalizationService is null
                ? $"Не удалось запустить приложение.{Environment.NewLine}{Environment.NewLine}{exception.Message}"
                : LocalizationService.Format("Startup.ErrorMessage", exception.Message);

            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
        }
    }
}
