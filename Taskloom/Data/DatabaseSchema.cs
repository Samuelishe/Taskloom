namespace Taskloom.Data;

/// <summary>
/// Имена таблиц, индексов и скриптов базы данных.
/// </summary>
public static class DatabaseSchema
{
    public const string CalendarRecordsTable = "calendar_records";

    public const string DateIndex = "ix_calendar_records_date";

    public const string DateTypeIndex = "ix_calendar_records_date_type";

    public const string DaySummaryUniqueIndex = "ux_calendar_records_day_summary_date";

    public const string CreateSchemaScriptPath = "Data/Sql/CreateSchema.sql";
}
