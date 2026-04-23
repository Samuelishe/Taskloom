using System.Windows;
using Taskloom.Infrastructure.Repositories;
using Taskloom.Infrastructure.Storage;
using Taskloom.Presentation.ViewModels;
using Taskloom.Presentation.Views;
using Taskloom.Services.Records;

namespace Taskloom;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var databasePath = TaskloomPaths.GetDatabasePath();
        var connectionFactory = new SqliteConnectionFactory(databasePath);
        var databaseInitializer = new SqliteDatabaseInitializer(connectionFactory);
        await databaseInitializer.InitializeAsync();

        var repository = new SqliteCalendarRecordRepository(connectionFactory);
        var recordService = new CalendarRecordService(repository);
        var mainWindowViewModel = new MainWindowViewModel(recordService);

        await mainWindowViewModel.InitializeAsync();

        var mainWindow = new MainWindow
        {
            DataContext = mainWindowViewModel
        };

        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
