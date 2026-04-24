namespace Taskloom.Services.Settings;

/// <summary>
/// Политика автоматической очистки старых записей при запуске приложения.
/// </summary>
public enum RecordCleanupMode
{
    Never = 0,
    DeleteAllOlderThan7Days = 1,
    DeleteAllOlderThan1Month = 2,
    DeleteCompletedAndPastOlderThan7Days = 3,
    DeleteCompletedAndPastOlderThan1Month = 4
}
