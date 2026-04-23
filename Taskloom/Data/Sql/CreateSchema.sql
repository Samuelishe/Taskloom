CREATE TABLE IF NOT EXISTS calendar_records
(
    id TEXT NOT NULL PRIMARY KEY,
    type_id INTEGER NOT NULL,
    record_date TEXT NOT NULL,
    title TEXT NOT NULL,
    details TEXT NULL,
    is_completed INTEGER NULL,
    start_time TEXT NULL,
    end_time TEXT NULL,
    location TEXT NULL,
    event_status_id INTEGER NULL,
    reminder_minutes_before INTEGER NULL,
    CHECK (type_id IN (1, 2, 3, 4)),
    CHECK (length(trim(title)) > 0),
    CHECK (length(title) <= 200),
    CHECK (details IS NULL OR length(details) <= 4000),
    CHECK (location IS NULL OR length(location) <= 300),
    CHECK (is_completed IS NULL OR is_completed IN (0, 1)),
    CHECK (start_time IS NULL OR length(start_time) = 5),
    CHECK (end_time IS NULL OR length(end_time) = 5),
    CHECK (event_status_id IS NULL OR event_status_id IN (1, 2, 3, 4)),
    CHECK (reminder_minutes_before IS NULL OR reminder_minutes_before >= 0),
    CHECK (
        type_id != 1
        OR (is_completed IS NOT NULL AND start_time IS NULL AND end_time IS NULL AND location IS NULL AND event_status_id IS NULL AND reminder_minutes_before IS NULL)
    ),
    CHECK (
        type_id != 2
        OR (is_completed IS NULL AND start_time IS NULL AND end_time IS NULL AND location IS NULL AND event_status_id IS NULL AND reminder_minutes_before IS NULL)
    ),
    CHECK (
        type_id != 3
        OR (is_completed IS NULL AND start_time IS NOT NULL AND end_time IS NOT NULL AND end_time > start_time AND event_status_id IS NOT NULL AND reminder_minutes_before IS NOT NULL)
    ),
    CHECK (
        type_id != 4
        OR (is_completed IS NULL AND start_time IS NULL AND end_time IS NULL AND location IS NULL AND event_status_id IS NULL AND reminder_minutes_before IS NULL)
    )
);

CREATE INDEX IF NOT EXISTS ix_calendar_records_date
    ON calendar_records(record_date);

CREATE INDEX IF NOT EXISTS ix_calendar_records_date_type
    ON calendar_records(record_date, type_id);

CREATE UNIQUE INDEX IF NOT EXISTS ux_calendar_records_day_summary_date
    ON calendar_records(record_date)
    WHERE type_id = 4;
