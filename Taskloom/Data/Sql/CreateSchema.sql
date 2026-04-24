CREATE TABLE IF NOT EXISTS calendar_records
(
    id TEXT NOT NULL PRIMARY KEY,
    type_id INTEGER NOT NULL,
    record_date TEXT NOT NULL,
    title TEXT NOT NULL,
    details TEXT NULL,
    created_utc TEXT NOT NULL,
    is_completed INTEGER NULL,
    task_reminder_time TEXT NULL,
    start_time TEXT NULL,
    end_time TEXT NULL,
    location TEXT NULL,
    event_status_id INTEGER NULL,
    reminder_minutes_before INTEGER NULL,
    CHECK (type_id IN (1, 2, 3, 4)),
    CHECK (length(trim(title)) > 0),
    CHECK (length(title) <= 200),
    CHECK (details IS NULL OR length(details) <= 4000),
    CHECK (length(created_utc) > 0),
    CHECK (location IS NULL OR length(location) <= 300),
    CHECK (is_completed IS NULL OR is_completed IN (0, 1)),
    CHECK (task_reminder_time IS NULL OR length(task_reminder_time) = 5),
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
        OR (is_completed IS NULL AND task_reminder_time IS NULL AND start_time IS NULL AND end_time IS NULL AND location IS NULL AND event_status_id IS NULL AND reminder_minutes_before IS NULL)
    ),
    CHECK (
        type_id != 3
        OR (is_completed IS NULL AND task_reminder_time IS NULL AND start_time IS NOT NULL AND end_time IS NOT NULL AND end_time > start_time AND event_status_id IS NOT NULL AND reminder_minutes_before IS NOT NULL)
    ),
    CHECK (
        type_id != 4
        OR (is_completed IS NULL AND task_reminder_time IS NULL AND start_time IS NULL AND end_time IS NULL AND location IS NULL AND event_status_id IS NULL AND reminder_minutes_before IS NULL)
    )
);

CREATE TABLE IF NOT EXISTS record_attachments
(
    id TEXT NOT NULL PRIMARY KEY,
    record_id TEXT NOT NULL,
    kind_id INTEGER NOT NULL,
    original_file_name TEXT NOT NULL,
    stored_file_name TEXT NOT NULL,
    relative_path TEXT NOT NULL,
    content_type TEXT NULL,
    file_size INTEGER NOT NULL,
    created_utc TEXT NOT NULL,
    sort_order INTEGER NOT NULL DEFAULT 0,
    display_title TEXT NULL,
    duration_seconds REAL NULL,
    preview_relative_path TEXT NULL,
    album_title TEXT NULL,
    genre TEXT NULL,
    CHECK (length(trim(original_file_name)) > 0),
    CHECK (length(original_file_name) <= 260),
    CHECK (length(trim(stored_file_name)) > 0),
    CHECK (length(stored_file_name) <= 260),
    CHECK (length(trim(relative_path)) > 0),
    CHECK (length(relative_path) <= 512),
    CHECK (content_type IS NULL OR length(content_type) <= 100),
    CHECK (display_title IS NULL OR length(display_title) <= 260),
    CHECK (preview_relative_path IS NULL OR length(preview_relative_path) <= 512),
    CHECK (album_title IS NULL OR length(album_title) <= 260),
    CHECK (genre IS NULL OR length(genre) <= 120),
    CHECK (file_size >= 0),
    CHECK (sort_order >= 0),
    CHECK (duration_seconds IS NULL OR duration_seconds >= 0)
);

CREATE INDEX IF NOT EXISTS ix_calendar_records_date
    ON calendar_records(record_date);

CREATE INDEX IF NOT EXISTS ix_calendar_records_date_type
    ON calendar_records(record_date, type_id);

CREATE UNIQUE INDEX IF NOT EXISTS ux_calendar_records_day_summary_date
    ON calendar_records(record_date)
    WHERE type_id = 4;

CREATE INDEX IF NOT EXISTS ix_record_attachments_record_id
    ON record_attachments(record_id);
