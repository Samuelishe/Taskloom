# Журнал работ

## 2026-04-24 - Ховер-подсказки строк и снятие выделения записи

### Что сделано

- Для обрезаемых строк в карточках записей добавлены tooltip-подсказки с полным значением: заголовок, место события, описание, title аудио, строка `album • genre` и имя файла.
- Бейдж времени создания приведён к общей системе chips: теперь он использует ту же геометрию и типографический стиль, но остаётся более приглушённым.
- Выделение записи переработано: вместо отдельной синей полосы карточка теперь получает утолщённую левую границу собственной рамки, поэтому исчезают пиксельные артефакты на скруглениях.
- Добавлено снятие выделения записи кликом по пустым областям окна и неинтерактивным поверхностям вне карточек.
- После снятия выделения `SelectedRecord` становится `null`, поэтому команды редактирования и удаления автоматически возвращаются в неактивное состояние.

### Изменённые файлы

- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/Presentation/Views/MainWindow.xaml.cs`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

## 2026-04-24 - Стабилизация настроек, карточек и подтверждающих окон

### Что сделано

- Исправлено выравнивание контента в карточках записей: короткий текст больше не центрируется по высоте рядом с более высокими карточками.
- Порядок блоков карточки закреплён как `заголовок -> текстовые блоки -> аудио -> изображения`.
- Исправлены гонки instant-apply в окне настроек: быстрые переключения темы и языка больше не должны запускать конфликтующие параллельные операции.
- `AppSettingsService` сериализует доступ к `settings.json`, поэтому сохранение настроек и placement главного окна больше не конфликтуют между собой.
- В `MainWindowViewModel` добавлена защита от временного `null` у фильтра при локализационной перестройке option-списков.
- После `Show()` главное окно теперь явно активируется, чтобы уменьшить риск сценария, где первый клик по `Новая запись` только активирует окно.
- Для подтверждений удаления записи и dangerous cleanup mode добавлено собственное themed-окно `ConfirmationDialogWindow` вместо системного `MessageBox`.
- Добавлены локализованные подписи `Да/Нет` и `Yes/No` для нового диалога.
- Сборка `dotnet build .\Taskloom.sln --no-restore` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `README.md`
- `Taskloom/App.xaml.cs`
- `Taskloom/Assets/Localization/ru-RU.json`
- `Taskloom/Assets/Localization/en-US.json`
- `Taskloom/Infrastructure/Settings/AppSettingsService.cs`
- `Taskloom/Presentation/ViewModels/MainWindowViewModel.cs`
- `Taskloom/Presentation/ViewModels/SettingsViewModel.cs`
- `Taskloom/Presentation/Views/ConfirmationDialogWindow.xaml`
- `Taskloom/Presentation/Views/ConfirmationDialogWindow.xaml.cs`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/Presentation/Views/MainWindow.xaml.cs`
- `Taskloom/Presentation/Views/SettingsWindow.xaml.cs`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/GitWorkflow.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Системный `MessageBox` визуально конфликтовал с theme-aware интерфейсом и не наследовал активную тему приложения.
- Параллельные сохранения `settings.json` уже приводили к падению приложения при закрытии окна после быстрых переключений настроек.
- Карточки записей должны сохранять естественный reading order сверху вниз, даже если соседние карточки в ряду выше.

## 2026-04-24 - Время создания записей и расширенные аудио-метаданные

### Что сделано

- Для всех записей добавлено persisted-поле `created_utc`.
- В карточке записи добавлен компактный слабоконтрастный бейдж времени создания `HH:mm`.
- Для audio attachments добавлены nullable-поля `album_title` и `genre`.
- `RecordAudioStorageService` теперь читает album и genre из тегов файла, если они есть.
- В карточке аудио рядом с названием трека показывается компактная muted-строка `album • genre`, только если метаданные реально присутствуют.
- Убрано смысловое дублирование `DisplayTitle + FileName`: имя файла скрывается, если оно совпадает с title полностью или по имени без расширения.
- Обновлены SQLite schema, миграции и маппинг repository/service/presentation-слоёв.
- Сборка `dotnet build .\Taskloom.sln --no-restore` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/Data/Models/CalendarRecordDataModel.cs`
- `Taskloom/Data/Models/RecordAttachmentDataModel.cs`
- `Taskloom/Data/Sql/CreateSchema.sql`
- `Taskloom/Domain/CalendarRecord.cs`
- `Taskloom/Domain/DaySummaryRecord.cs`
- `Taskloom/Domain/EventRecord.cs`
- `Taskloom/Domain/NoteRecord.cs`
- `Taskloom/Domain/RecordAttachment.cs`
- `Taskloom/Domain/TaskRecord.cs`
- `Taskloom/Infrastructure/Repositories/SqliteCalendarRecordRepository.cs`
- `Taskloom/Infrastructure/Storage/RecordAudioStorageService.cs`
- `Taskloom/Infrastructure/Storage/SqliteDatabaseInitializer.cs`
- `Taskloom/Presentation/ViewModels/RecordAudioListItemViewModel.cs`
- `Taskloom/Presentation/ViewModels/RecordEditorViewModel.cs`
- `Taskloom/Presentation/ViewModels/RecordListItemViewModel.cs`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/Services/Records/CalendarRecordService.cs`
- `Taskloom/Services/Records/RecordAudioDraft.cs`
- `README.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Пользователь должен видеть момент создания записи без тяжёлого визуального акцента и без шума в карточке.
- Audio metadata полезна только как лёгкая дополнительная подсказка; при отсутствии album/genre контейнер плеера не должен заметно раздуваться.
- Дублирование заголовка трека и имени файла делает карточку визуально тяжелее и не добавляет пользы.

## 2026-04-24 - Startup cleanup, instant-apply settings и per-record resource folders

### Что сделано

- Добавлен enum `RecordCleanupMode` и новое поле `RecordCleanupMode` в `AppSettings`.
- Окно настроек переведено на instant apply: язык, тема и политика очистки применяются сразу при выборе; отдельная кнопка `Применить` удалена.
- Для опасных режимов удаления всех старых записей добавлено подтверждение через `MessageBox` и откат выбора при отказе.
- Добавлен startup cleanup старых записей через `CalendarRecordService.CleanupOldRecordsAsync`.
- Введены пять режимов очистки: `Never`, удаление всех записей старше `7 дней` или `1 месяца`, а также два безопасных режима только для завершённых задач и прошедших событий.
- Файловые ресурсы записи перенесены в отдельные каталоги `%LocalAppData%\Taskloom\Records\<record-id>\images`, `audio`, `audio-covers`.
- Добавлен `RecordResourceMetadataService`, который поддерживает metadata-файл `%LocalAppData%\Taskloom\Records\<record-id>\record.json`.
- Удаление записи теперь очищает и SQLite, и файловый каталог записи.
- Startup cleanup защищён от падения всего приложения: ошибки пишутся в `%LocalAppData%\Taskloom\record-cleanup.log`.
- Обновлены `README.md` и проектная документация.

### Изменённые и созданные файлы

- `README.md`
- `Taskloom/App.xaml.cs`
- `Taskloom/Assets/Localization/ru-RU.json`
- `Taskloom/Assets/Localization/en-US.json`
- `Taskloom/Infrastructure/Repositories/SqliteCalendarRecordRepository.cs`
- `Taskloom/Infrastructure/Storage/RecordAudioStorageService.cs`
- `Taskloom/Infrastructure/Storage/RecordImageStorageService.cs`
- `Taskloom/Infrastructure/Storage/RecordResourceMetadataService.cs`
- `Taskloom/Infrastructure/Storage/TaskloomDiagnosticLog.cs`
- `Taskloom/Infrastructure/Storage/TaskloomPaths.cs`
- `Taskloom/Presentation/ViewModels/CleanupModeOptionViewModel.cs`
- `Taskloom/Presentation/ViewModels/SettingsViewModel.cs`
- `Taskloom/Presentation/Views/SettingsWindow.xaml`
- `Taskloom/Presentation/Views/SettingsWindow.xaml.cs`
- `Taskloom/Services/Records/CalendarRecordService.cs`
- `Taskloom/Services/Records/ICalendarRecordRepository.cs`
- `Taskloom/Services/Records/ICalendarRecordService.cs`
- `Taskloom/Services/Records/IRecordResourceMetadataService.cs`
- `Taskloom/Services/Settings/AppSettings.cs`
- `Taskloom/Services/Settings/RecordCleanupMode.cs`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Auto-cleanup должен быть частью настроек приложения, а не разовой ручной операцией.
- Перевод вложений на каталоги конкретной записи упрощает полное удаление ресурсов и готовит почву для будущих export/import сценариев.
- Instant apply в настройках уменьшает лишние действия пользователя и делает тему/язык/cleanup policy более предсказуемыми.

## 2026-04-23 - Инициализация структуры проекта

### Что сделано

- Проанализирован стартовый состав solution и WPF-проекта.
- Создана папка `docs` и комплект обязательной документации.
- Создана базовая структура каталогов для логического разделения проекта.
- Стартовое окно перенесено в слой `Presentation/Views`.
- Добавлена минимальная `MainWindowViewModel` для стартового MVVM-каркаса.

### Изменённые и созданные файлы

- `Taskloom/Taskloom.csproj`
- `Taskloom/App.xaml`
- `Taskloom/App.xaml.cs`
- `Taskloom/AssemblyInfo.cs`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/Presentation/Views/MainWindow.xaml.cs`
- `Taskloom/Presentation/ViewModels/MainWindowViewModel.cs`
- `docs/ProjectOverview.md`
- `docs/DevelopmentPlan.md`
- `docs/WorkLog.md`
- `docs/Architecture.md`
- `docs/Decisions.md`

### Обоснование

- На старте выбран один WPF-проект с логическим разделением по папкам, потому что для MVP это самый простой и устойчивый вариант.
- Полноценная доменная и data-модель отложены на следующий шаг, чтобы сначала зафиксировать структуру и документацию.

## 2026-04-23 - Корректировка структуры для Rider

### Что сделано

- Папка `docs` перенесена внутрь каталога проекта `Taskloom`.
- В пустые верхнеуровневые каталоги добавлены `README.md`, чтобы структура отображалась в панели Rider.

### Изменённые и созданные файлы

- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/WorkLog.md`
- `Taskloom/Assets/README.md`
- `Taskloom/Common/README.md`
- `Taskloom/Data/README.md`
- `Taskloom/Domain/README.md`
- `Taskloom/Infrastructure/README.md`
- `Taskloom/Services/README.md`

### Обоснование

- В текущей конфигурации IDE у пользователя открыт именно каталог проекта, а не корень solution.
- Размещение документации внутри видимого проекта и добавление файлов-маркеров делает структуру прозрачной без изменения архитектурной модели приложения.

## 2026-04-23 - Формирование доменной модели

### Что сделано

- Введён enum `RecordType` для поддержки нескольких типов календарных записей.
- Создана базовая абстрактная сущность `CalendarRecord`.
- Реализованы специализированные доменные типы: `TaskRecord`, `NoteRecord`, `EventRecord`, `DaySummaryRecord`.
- Зафиксированы базовые инварианты: обязательный заголовок, нормализация текста, проверка временного диапазона события.
- Обновлена документация по архитектуре и плану разработки.

### Изменённые и созданные файлы

- `Taskloom/Domain/RecordType.cs`
- `Taskloom/Domain/CalendarRecord.cs`
- `Taskloom/Domain/TaskRecord.cs`
- `Taskloom/Domain/NoteRecord.cs`
- `Taskloom/Domain/EventRecord.cs`
- `Taskloom/Domain/DaySummaryRecord.cs`
- `Taskloom/Domain/README.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Иерархия доменных типов лучше соответствует концепции приложения, чем одна универсальная запись с набором необязательных полей.
- Базовые проверки нужно держать в домене, чтобы не размазывать правила по UI и будущему data access слою.

## 2026-04-23 - Подготовка контекста продолжения

### Что сделано

- Создан отдельный файл `ContinuationGuide.md` для восстановления контекста в новой сессии.
- В этом файле зафиксированы технологические ограничения, правила коммуникации, текущая стадия проекта и следующий шаг.
- Обновлены документы, которые должны направлять новую сессию на правильную точку продолжения.

### Изменённые и созданные файлы

- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Если сессия пропадёт, следующему исполнителю нужен явный документ входа, а не только разрозненные сведения по нескольким файлам.
- Правила проекта и точка остановки должны быть зафиксированы внутри `docs`, а не зависеть от памяти текущей сессии.

## 2026-04-23 - Проектирование data model и SQLite schema

### Что сделано

- Спроектирована storage-модель `CalendarRecordDataModel` для SQLite и Dapper.
- Добавлен SQL-скрипт `CreateSchema.sql` для создания таблицы `calendar_records`.
- В схему включены индексы по дате и типу, а также уникальный partial index для `DaySummaryRecord`.
- На уровне SQL зафиксированы основные ограничения для типов записей.
- Обновлены архитектурные документы и точка продолжения работы.

### Изменённые и созданные файлы

- `Taskloom/Taskloom.csproj`
- `Taskloom/Data/Models/CalendarRecordDataModel.cs`
- `Taskloom/Data/DatabaseSchema.cs`
- `Taskloom/Data/Sql/CreateSchema.sql`
- `Taskloom/Data/README.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Для MVP одна таблица под все типы записей даёт лучший баланс между простотой схемы и выразительностью домена.
- Основные ограничения нужно зафиксировать не только в домене, но и в БД, чтобы уменьшить риск неконсистентных данных.

## 2026-04-23 - Реализация инфраструктуры SQLite и репозитория

### Что сделано

- Подключены пакеты `Microsoft.Data.Sqlite` и `Dapper`.
- Реализована фабрика подключений `SqliteConnectionFactory`.
- Реализован инициализатор базы `SqliteDatabaseInitializer`, применяющий `CreateSchema.sql`.
- Добавлен `TaskloomPaths` для стандартного пути к локальной SQLite-базе.
- Определён интерфейс `ICalendarRecordRepository`.
- Реализован `SqliteCalendarRecordRepository` с CRUD-операциями и маппингом между storage-моделью и доменом.
- Обновлена документация и точка продолжения работы.

### Изменённые и созданные файлы

- `Taskloom/Taskloom.csproj`
- `Taskloom/Infrastructure/Storage/SqliteConnectionFactory.cs`
- `Taskloom/Infrastructure/Storage/SqliteDatabaseInitializer.cs`
- `Taskloom/Infrastructure/Storage/TaskloomPaths.cs`
- `Taskloom/Infrastructure/Repositories/SqliteCalendarRecordRepository.cs`
- `Taskloom/Services/Records/ICalendarRecordRepository.cs`
- `Taskloom/Infrastructure/README.md`
- `Taskloom/Services/README.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/WorkLog.md`
- `Taskloom/docs/ContinuationGuide.md`

### Обоснование

- Persistence-слой должен быть готов до разработки прикладных сервисов и ViewModels.
- На этом этапе полезнее иметь рабочий репозиторий с явным маппингом, чем преждевременно строить сложную инфраструктурную абстракцию.

## 2026-04-23 - Реализация прикладного слоя записей

### Что сделано

- Добавлен `CalendarRecordDraft` для сценариев создания и редактирования записей.
- Определён интерфейс `ICalendarRecordService`.
- Реализован `CalendarRecordService` для CRUD, фильтрации по типу и преобразования записи в черновик.
- Обновлена документация и новая точка продолжения работ.

### Изменённые и созданные файлы

- `Taskloom/Services/Records/CalendarRecordDraft.cs`
- `Taskloom/Services/Records/ICalendarRecordService.cs`
- `Taskloom/Services/Records/CalendarRecordService.cs`
- `Taskloom/Services/README.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/WorkLog.md`
- `Taskloom/docs/ContinuationGuide.md`

### Обоснование

- Будущие ViewModels не должны заниматься конструированием доменных типов напрямую.
- Draft-модель даёт чистую точку входа для окна редактора без утечки persistence-деталей в UI.

## 2026-04-23 - Реализация ViewModels и композиции приложения

### Что сделано

- Подключён `CommunityToolkit.Mvvm`.
- `App` переведён на явную композицию зависимостей и инициализацию SQLite при старте.
- Реализован `MainWindowViewModel` для выбранной даты, фильтрации, списка записей и команд.
- Реализован `RecordEditorViewModel`.
- Добавлены presentation-модели `RecordListItemViewModel` и `RecordTypeFilterOptionViewModel`.
- Стартовый `MainWindow.xaml` приведён в соответствие новым свойствам ViewModel.

### Изменённые и созданные файлы

- `Taskloom/Taskloom.csproj`
- `Taskloom/App.xaml`
- `Taskloom/App.xaml.cs`
- `Taskloom/Presentation/ViewModels/MainWindowViewModel.cs`
- `Taskloom/Presentation/ViewModels/RecordEditorViewModel.cs`
- `Taskloom/Presentation/ViewModels/RecordListItemViewModel.cs`
- `Taskloom/Presentation/ViewModels/RecordTypeFilterOptionViewModel.cs`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/WorkLog.md`
- `Taskloom/docs/ContinuationGuide.md`

### Обоснование

- ViewModels должны работать через прикладной слой, а не напрямую через инфраструктуру или XAML-конструкторы.
- Явная композиция в `App` сильнее шаблонного `StartupUri`, когда приложению уже нужны сервисы и инициализация базы.

## 2026-04-23 - Реализация WPF Views для MVP

### Что сделано

- Переработан `MainWindow.xaml` в полноценный экран с календарём, фильтром, действиями и списком записей.
- Добавлено отдельное окно `RecordEditorWindow`.
- В `MainWindow.xaml.cs` реализована тонкая оркестрация открытия и закрытия окна редактора.
- `RecordEditorViewModel` расширен командами сохранения и отмены, а `MainWindowViewModel` получил связку с редактором.
- Обновлена документация и точка продолжения работы.

### Изменённые и созданные файлы

- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/Presentation/Views/MainWindow.xaml.cs`
- `Taskloom/Presentation/Views/RecordEditorWindow.xaml`
- `Taskloom/Presentation/Views/RecordEditorWindow.xaml.cs`
- `Taskloom/Presentation/ViewModels/MainWindowViewModel.cs`
- `Taskloom/Presentation/ViewModels/RecordEditorViewModel.cs`
- `Taskloom/Presentation/ViewModels/RecordEditorCloseRequestedEventArgs.cs`
- `Taskloom/Presentation/ViewModels/RecordTypeFilterOptionViewModel.cs`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/WorkLog.md`
- `Taskloom/docs/ContinuationGuide.md`

### Обоснование

- Главное окно и отдельный редактор закрывают основной UI-контур MVP.
- View остаются тонкими: логика открытия окна допустима во View, но логика сохранения и валидации удержана в MVVM и сервисах.

## 2026-04-23 - Подготовка Git-правил и репозитория

### Что сделано

- Актуализирован `.gitignore` под .NET, WPF, Rider и локальные SQLite-артефакты.
- В документацию добавлены правила работы с Git и GitHub.
- Зафиксировано, что `docs` должны входить в коммиты наравне с кодом.

### Изменённые и созданные файлы

- `.gitignore`
- `Taskloom/docs/GitWorkflow.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/WorkLog.md`
- `Taskloom/docs/ContinuationGuide.md`

### Обоснование

- Проект уже зависит от документации как от части процесса разработки, значит она должна быть обязательной частью коммитов.
- Нужен предсказуемый git-поток: рабочая ветка `dev`, чистый `.gitignore` и явные правила staged-состава.

## 2026-04-23 - Рефайнмент MVP-сценариев

### Что сделано

- Добавлено подтверждение удаления записи.
- Добавлено открытие редактора по двойному клику на запись.
- Устранён хрупкий сценарий с вводом времени: теперь время валидируется при сохранении.
- Startup приложения переведён на безопасный режим с сообщением об ошибке пользователю.
- Финальная документация MVP синхронизирована.

### Изменённые и созданные файлы

- `Taskloom/App.xaml.cs`
- `Taskloom/Presentation/ViewModels/RecordEditorViewModel.cs`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/Presentation/Views/MainWindow.xaml.cs`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/WorkLog.md`
- `Taskloom/docs/ContinuationGuide.md`

### Обоснование

- Эти правки закрывают реальные слабые места MVP без перестройки архитектуры.
- После них сценарии `create/edit/delete` стали устойчивее и ближе к рабочему desktop-приложению.

## 2026-04-23 - Планирование локализации и настроек

### Что сделано

- Согласован следующий этап: локализация интерфейса и окно настроек.
- Зафиксировано требование хранить языки в отдельных файлах.
- Зафиксировано требование поддержать `ru-RU` и `en-US`.
- Зафиксировано, что настройки приложения должны храниться отдельно от SQLite.
- Зафиксировано решение открывать настройки из главного окна по кнопке-шестерёнке.
- В план добавлено увеличение календаря как ближайший UX-рефайнмент.

### Изменённые и созданные файлы

- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/WorkLog.md`
- `Taskloom/docs/ContinuationGuide.md`

### Обоснование

- Локализацию нужно внедрять сейчас, пока presentation-слой ещё не разросся.
- Настройки языка и размер календаря лучше зафиксировать заранее как часть согласованного направления развития UI.

## 2026-04-23 - Реализация локализации и настроек

### Что сделано

- Добавлена инфраструктура локализации на основе JSON-файлов `ru-RU` и `en-US`.
- Добавлен `Loc`-механизм для XAML и сервис локализации для ViewModels и сообщений UI.
- Добавлен отдельный settings-сервис и `settings.json` вне SQLite.
- Реализовано окно настроек и кнопка-шестерёнка в главном окне.
- Текущий UI переведён на локализованные строки.
- Календарь увеличен как часть UX-рефайнмента главного окна.

### Изменённые и созданные файлы

- `Taskloom/Taskloom.csproj`
- `Taskloom/Assets/Localization/ru-RU.json`
- `Taskloom/Assets/Localization/en-US.json`
- `Taskloom/Assets/README.md`
- `Taskloom/Common/Markup/LocExtension.cs`
- `Taskloom/Common/README.md`
- `Taskloom/Services/Localization/ILocalizationService.cs`
- `Taskloom/Services/Settings/AppSettings.cs`
- `Taskloom/Services/Settings/IAppSettingsService.cs`
- `Taskloom/Services/README.md`
- `Taskloom/Infrastructure/Localization/LocalizationSource.cs`
- `Taskloom/Infrastructure/Localization/LocalizationManager.cs`
- `Taskloom/Infrastructure/Localization/LocalizationService.cs`
- `Taskloom/Infrastructure/Settings/AppSettingsService.cs`
- `Taskloom/Infrastructure/Storage/TaskloomPaths.cs`
- `Taskloom/Infrastructure/README.md`
- `Taskloom/App.xaml.cs`
- `Taskloom/Presentation/ViewModels/LanguageOptionViewModel.cs`
- `Taskloom/Presentation/ViewModels/SettingsCloseRequestedEventArgs.cs`
- `Taskloom/Presentation/ViewModels/SettingsViewModel.cs`
- `Taskloom/Presentation/ViewModels/MainWindowViewModel.cs`
- `Taskloom/Presentation/ViewModels/RecordEditorViewModel.cs`
- `Taskloom/Presentation/ViewModels/RecordListItemViewModel.cs`
- `Taskloom/Presentation/ViewModels/RecordTypeFilterOptionViewModel.cs`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/Presentation/Views/MainWindow.xaml.cs`
- `Taskloom/Presentation/Views/RecordEditorWindow.xaml`
- `Taskloom/Presentation/Views/SettingsWindow.xaml`
- `Taskloom/Presentation/Views/SettingsWindow.xaml.cs`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/WorkLog.md`
- `Taskloom/docs/ContinuationGuide.md`

### Обоснование

- Локализация внедрена до дальнейшего роста UI, поэтому проект не уходит в хаотичный хардкод строк.
- Настройки языка и локализация встроены в архитектуру как обязательный слой, а не как поздняя надстройка.

## 2026-04-23 - Исправление критических падений локализованного MVP

### Что сделано

- Исправлено падение при открытии окна настроек из-за раннего вызова `SaveCommand.NotifyCanExecuteChanged()`.
- Исправлено падение при отображении списка записей после создания задачи.
- Для `CheckBox` в шаблоне списка записей задан `Mode=OneWay`, потому что `RecordListItemViewModel.IsCompleted` является свойством только для чтения.
- Повторно проверена сборка проекта после исправлений.

### Изменённые и созданные файлы

- `Taskloom/Presentation/ViewModels/SettingsViewModel.cs`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/docs/WorkLog.md`
- `Taskloom/docs/ContinuationGuide.md`

### Обоснование

- Оба дефекта приводили к аварийному завершению процесса без нормального пользовательского сценария восстановления.
- Исправление привязки в `MainWindow.xaml` минимально по объёму, соответствует MVVM и устраняет причину, а не симптом.

## 2026-04-23 - UX-рефайнмент списка записей

### Что сделано

- Список записей переработан из однотипной вертикальной ленты в адаптивный карточный layout.
- Добавлен converter для вычисления ширины карточек по доступной ширине списка и количеству записей.
- Убрано стандартное синее системное выделение `ListBoxItem`.
- Добавлены более мягкие состояния hover и selection, согласованные с текущей тёплой темой интерфейса.
- Карточки теперь перестраиваются при ресайзе окна и лучше используют горизонтальное пространство.

### Изменённые и созданные файлы

- `Taskloom/Common/Converters/AdaptiveCardWidthConverter.cs`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/WorkLog.md`
- `Taskloom/docs/ContinuationGuide.md`

### Обоснование

- Текущий UI плохо использовал ширину окна и визуально опирался на системные стили, которые конфликтовали с общей темой приложения.
- Карточная адаптивная раскладка даёт более desktop-ориентированное поведение без перестройки домена, сервисов и ViewModels.

## 2026-04-23 - Продолжение UX-рефайнмента главного окна

### Что сделано

- Исправлено растягивание бейджа типа записи на всю ширину карточки.
- Переработаны верхняя панель главного окна и кнопка настроек.
- Улучшен блок выбранной даты и статуса.
- Календарь помещён в более аккуратный контейнер, визуально согласованный с остальными блоками.
- Кнопки действий переведены на более мягкий единый стиль.

### Изменённые и созданные файлы

- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/WorkLog.md`
- `Taskloom/docs/ContinuationGuide.md`

### Обоснование

- После переделки списка записей остальная часть главного окна выглядела визуально слабее и выбивалась по стилю.
- Эти правки улучшают цельность интерфейса без создания лишнего слоя темы или переусложнения XAML-инфраструктуры.

## 2026-04-23 - Корректировка приоритета календаря и ширины одиночных карточек

### Что сделано

- Увеличены размеры и минимальные габариты главного окна.
- Левой колонке с календарём выделено больше ширины.
- Масштаб календаря заметно увеличен, чтобы он соответствовал роли главного элемента приложения.
- В converter ширины карточек добавлено верхнее ограничение, чтобы одиночная запись не растягивалась на всю ширину списка.

### Изменённые и созданные файлы

- `Taskloom/Common/Converters/AdaptiveCardWidthConverter.cs`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/WorkLog.md`
- `Taskloom/docs/ContinuationGuide.md`

### Обоснование

- Для calendar-centered приложения прежний размер календаря был архитектурно слабым и визуально второстепенным.
- Растягивание одиночной карточки ухудшало композицию списка и делало layout менее стабильным.

## 2026-04-23 - Планирование calendar-first layout и цветовых схем

### Что сделано

- Зафиксировано новое направление развития главного окна: `calendar-first` layout.
- Зафиксировано требование сделать визуально явное состояние выбранной записи.
- Согласован отдельный слой тем оформления для основных цветов интерфейса.
- Выбран подход с использованием `ResourceDictionary` для цветовых схем и хранением активной темы в настройках приложения.
- Обновлены план разработки, архитектурные правила и точка продолжения.

### Изменённые и созданные файлы

- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Без формализации следующего шага UI-решения начали бы развиваться реактивно и фрагментарно.
- Theme infrastructure и calendar-first компоновка должны быть сначала зафиксированы архитектурно, а уже потом реализованы в коде.

## 2026-04-23 - Реализация calendar-first layout и color themes

### Что сделано

- Перекомпоновано главное окно вокруг календаря: навигация по дню поднята выше, а календарь занимает основное пространство левой колонки.
- Усилен selected-state карточек записей: добавлена заметная цветовая полоса и более контрастное состояние выбора.
- Добавлен `ThemeService` и набор стартовых тем на основе `ResourceDictionary`.
- Настройки приложения расширены полем `ThemeId`.
- В окно настроек добавлен выбор цветовой схемы.
- Основные цвета `MainWindow`, `SettingsWindow` и `RecordEditorWindow` переведены на `DynamicResource`.

### Изменённые и созданные файлы

- `Taskloom/App.xaml`
- `Taskloom/App.xaml.cs`
- `Taskloom/Assets/Localization/ru-RU.json`
- `Taskloom/Assets/Localization/en-US.json`
- `Taskloom/Assets/Themes/WarmLightTheme.xaml`
- `Taskloom/Assets/Themes/NeutralLightTheme.xaml`
- `Taskloom/Infrastructure/Theming/ThemeIds.cs`
- `Taskloom/Infrastructure/Theming/ThemeService.cs`
- `Taskloom/Presentation/ViewModels/MainWindowViewModel.cs`
- `Taskloom/Presentation/ViewModels/SettingsViewModel.cs`
- `Taskloom/Presentation/ViewModels/ThemeOptionViewModel.cs`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/Presentation/Views/SettingsWindow.xaml`
- `Taskloom/Presentation/Views/RecordEditorWindow.xaml`
- `Taskloom/Services/Settings/AppSettings.cs`
- `Taskloom/Services/Theming/IThemeService.cs`
- `Taskloom/Services/Theming/ThemeDefinition.cs`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Без theme layer дальнейший UI-рефайнмент снова начал бы множить локальные цвета и стили.
- Calendar-first компоновка и тема оформления должны были войти в проект как связанное архитектурное изменение, а не как набор разрозненных косметических патчей.

## 2026-04-23 - Удаление псевдо-интерактивных галочек из карточек задач

### Что сделано

- Из карточек задач удалён `CheckBox`, который визуально выглядел как интерактивный, но по факту был только read-only индикатором.
- Сохранён единый источник отображения состояния задачи через текстовый статус в карточке.

### Изменённые и созданные файлы

- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Псевдо-интерактивный элемент вводил в заблуждение: пользователь ожидал переключения прямо из списка, которого в текущем UX нет.
- Для текущей модели редактирования честнее показывать состояние задачи как индикатор, а не как недоступный элемент управления.

## 2026-04-23 - Планирование quick toggle статуса задач

### Что сделано

- Зафиксировано решение заменить пассивный индикатор статуса задачи на интерактивный status chip.
- Зафиксировано требование перевести бейджи карточек на прямоугольную форму с умеренным скруглением.
- Зафиксировано требование использовать семантические success/danger цвета через темы.

### Изменённые и созданные файлы

- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Быстрый toggle статуса задачи логически относится к списку и должен быть оформлен как явный action-state элемент.
- Семантические цвета статусов должны зависеть от темы, а не быть захардкожены в карточке.

## 2026-04-23 - Реализация quick toggle статуса задач и status chips

### Что сделано

- В прикладной сервис добавлен сценарий быстрого переключения статуса задачи.
- В `MainWindowViewModel` добавлена команда переключения статуса задачи прямо из карточки списка.
- Для карточек задач добавлен интерактивный status chip с иконкой и текстом.
- Бейджи карточек переведены на прямоугольную форму с умеренным скруглением.
- В темы добавлены семантические success/danger ресурсы для статусных элементов.

### Изменённые и созданные файлы

- `Taskloom/Services/Records/ICalendarRecordService.cs`
- `Taskloom/Services/Records/CalendarRecordService.cs`
- `Taskloom/Presentation/ViewModels/MainWindowViewModel.cs`
- `Taskloom/Presentation/ViewModels/RecordListItemViewModel.cs`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/Assets/Localization/ru-RU.json`
- `Taskloom/Assets/Localization/en-US.json`
- `Taskloom/Assets/Themes/WarmLightTheme.xaml`
- `Taskloom/Assets/Themes/NeutralLightTheme.xaml`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Для задач быстрый toggle в списке полезнее, чем обязательный заход в окно редактора.
- Интерактивный status chip честнее и понятнее, чем декоративный индикатор, внешне похожий на control.

## 2026-04-23 - Планирование visual refinement chips и semantic palettes

### Что сделано

- Зафиксировано требование согласовать success/danger палитры с каждой темой, а не использовать механические красный и зелёный.
- Зафиксировано требование вынести общий стиль chips в общие ресурсы приложения.
- Зафиксировано решение ослабить лишние акценты карточек, чтобы status chip не конфликтовал с остальной композицией.

### Изменённые и созданные файлы

- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- На текущем этапе проблема уже не в архитектуре сценария, а в несогласованном visual language chips и карточек.
- Централизация geometry и palettes в ресурсах темы сильнее, чем точечные правки отдельных цветов в XAML.

## 2026-04-23 - Реализация visual refinement chips и semantic palettes

### Что сделано

- В `App.xaml` добавлены общие стили для нейтральных chips, текста chips и status chips.
- Для обеих тем пересобраны semantic palettes success/danger в более спокойных и согласованных оттенках.
- Type, info и status chips приведены к общей геометрии.
- Ослаблены лишние акценты карточек: убран постоянный цветовой контур по типу записи и уменьшена ширина selection stripe.

### Изменённые и созданные файлы

- `Taskloom/App.xaml`
- `Taskloom/Assets/Themes/WarmLightTheme.xaml`
- `Taskloom/Assets/Themes/NeutralLightTheme.xaml`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Визуальная система chips должна задаваться централизованно, иначе любая следующая правка статусов или бейджей снова ломает согласованность интерфейса.
- Более спокойные semantic palettes лучше сочетаются с карточками и не выбиваются из характера выбранной темы.

## 2026-04-23 - Custom title bar главного окна

### Что сделано

- Убрана стандартная системная шапка `MainWindow`.
- Добавлена кастомная title bar область в стиле текущей темы.
- Добавлены кнопки свернуть, развернуть/восстановить и закрыть.
- Добавлена возможность перетаскивать окно за кастомную шапку.
- Добавлен double click по шапке для maximize/restore.
- Для сохранения resize и desktop-поведения используется `WindowChrome`.

### Изменённые и созданные файлы

- `Taskloom/App.xaml`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/Presentation/Views/MainWindow.xaml.cs`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Стандартный title bar визуально выбивался из приложения.
- `WindowChrome` даёт нужную кастомизацию без отказа от нормального resize/snap поведения Windows.

## 2026-04-23 - Упрощение левой панели после добавления custom title bar

### Что сделано

- Убран дублирующий брендовый блок `Taskloom` и описание из левой панели.
- Кастомная шапка усилена типографически и стала основным местом бренда и текущей даты.
- В левой панели оставлен рабочий блок выбранной даты, потому что он связан с навигацией по дню и статусом записей.

### Изменённые и созданные файлы

- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/docs/WorkLog.md`
- `Taskloom/docs/Architecture.md`

### Обоснование

- После появления кастомной шапки бренд и описание в левой панели стали визуальным дублем.
- Дата в шапке и дата в рабочем блоке выполняют разные роли: глобальный контекст окна и управление выбранным днём.

## 2026-04-23 - Доводка расположения настроек и календарной области

### Что сделано

- Кнопка настроек перенесена из левой панели в кастомную шапку рядом с оконными кнопками.
- Убран лишний бейдж `Выбранная дата` из левой панели.
- Уменьшены отступы календарного контейнера, чтобы он не выглядел чрезмерно широким вокруг самого календаря.
- Убран пустой верхний ряд в левой панели после переноса настроек.

### Изменённые и созданные файлы

- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/docs/WorkLog.md`
- `Taskloom/docs/Architecture.md`

### Обоснование

- Настройки относятся к окну приложения в целом, поэтому в кастомной шапке они логичнее, чем рядом с выбранной датой.
- Бейдж выбранной даты был лишним после усиления даты в шапке и рабочем блоке.
- Календарный контейнер должен помогать календарю, а не создавать лишнюю пустую рамку вокруг него.

## 2026-04-23 - Упрощение календарной панели

### Что сделано

- Убран отдельный верхний контейнер с датой в левой панели.
- Дата оставлена в кастомной шапке и в правом заголовке списка записей.
- Статус и кнопки `Предыдущий день` / `Следующий день` перенесены в общий внешний контейнер календарной панели.
- Убран отдельный внутренний контейнер календаря, чтобы не создавать лишнюю рамку вокруг `Calendar`.

### Изменённые и созданные файлы

- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/docs/WorkLog.md`
- `Taskloom/docs/Architecture.md`

### Обоснование

- Левая панель должна быть компактной и calendar-first, без контейнеров внутри контейнеров.
- Дублирующая дата в блоке навигации создавала лишний визуальный вес, потому что текущая дата уже видна в шапке и в списке записей.

## 2026-04-23 - Доводка календарной панели и выбор времени события

### Что сделано

- Левая календарная панель перестроена в более устойчивую композицию: статус сверху, календарь по центру, навигационные кнопки снизу.
- Для стандартного `Calendar` добавлены локальные прозрачные ресурсы, чтобы убрать белую подложку/рамку вокруг календаря.
- Ручной ввод времени события заменён на `ComboBox` с 15-минутными интервалами.
- Убраны свойства `StartTimeText` и `EndTimeText` из `RecordEditorViewModel`.
- Убраны локализационные ключи старой подсказки и валидации формата `HH:mm`.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/Presentation/Views/RecordEditorWindow.xaml`
- `Taskloom/Presentation/ViewModels/RecordEditorViewModel.cs`
- `Taskloom/Presentation/ViewModels/TimeOptionViewModel.cs`
- `Taskloom/Assets/Localization/ru-RU.json`
- `Taskloom/Assets/Localization/en-US.json`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Навигация по дням и статус относятся к календарной панели, но им нужна понятная иерархия, иначе панель выглядит собранной случайно.
- Для события время должно выбираться интерфейсом, а не вводиться строкой с ручной проверкой формата.

## 2026-04-23 - Отображение места события в карточке

### Что сделано

- В `RecordListItemViewModel` добавлено отображаемое поле `LocationDisplay` и признак `HasLocation`.
- Для `EventRecord` место теперь форматируется через локализацию и выводится в карточке события.
- В XAML карточки добавлена отдельная строка места между заголовком и описанием.
- Добавлены локализационные ключи `RecordList.EventLocation` для русского и английского языков.
- В план добавлены дальнейшие идеи развития событий: длительность, напоминания, повторы, категории, ссылки и участники.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/Presentation/ViewModels/RecordListItemViewModel.cs`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/Assets/Localization/ru-RU.json`
- `Taskloom/Assets/Localization/en-US.json`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Если приложение позволяет указать место события, эта информация должна быть видна в основном календарном сценарии.
- Расширять событие новыми persisted-полями без решения по миграциям SQLite преждевременно.

## 2026-04-23 - Статусы событий и напоминания Windows

### Что сделано

- Добавлен доменный enum `EventStatus`: запланировано, прошло, перенесено, отменено.
- `EventRecord` расширен статусом события и временем напоминания до начала события.
- `CalendarRecordDraft`, `CalendarRecordDataModel`, SQLite schema и репозиторий расширены persisted-полями события.
- Для существующей SQLite базы добавлена проверка и добавление колонок `event_status_id` и `reminder_minutes_before`.
- В редактор события добавлены селекторы статуса и напоминания; значение по умолчанию для напоминания: за 1 час.
- В карточке события добавлен цветной status chip.
- В темы добавлены semantic resources для info/warning статусов.
- Добавлен `WindowsBalloonEventReminderService`, который показывает Windows-напоминание через `NotifyIcon.ShowBalloonTip` для запланированных событий.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/Taskloom.csproj`
- `Taskloom/App.xaml.cs`
- `Taskloom/Domain/EventStatus.cs`
- `Taskloom/Domain/EventRecord.cs`
- `Taskloom/Services/Records/CalendarRecordDraft.cs`
- `Taskloom/Services/Notifications/IEventReminderService.cs`
- `Taskloom/Infrastructure/Notifications/WindowsBalloonEventReminderService.cs`
- `Taskloom/Infrastructure/Storage/SqliteDatabaseInitializer.cs`
- `Taskloom/Infrastructure/Repositories/SqliteCalendarRecordRepository.cs`
- `Taskloom/Data/Models/CalendarRecordDataModel.cs`
- `Taskloom/Data/Sql/CreateSchema.sql`
- `Taskloom/Presentation/ViewModels/EventStatusOptionViewModel.cs`
- `Taskloom/Presentation/ViewModels/ReminderOptionViewModel.cs`
- `Taskloom/Presentation/ViewModels/RecordEditorViewModel.cs`
- `Taskloom/Presentation/ViewModels/RecordListItemViewModel.cs`
- `Taskloom/Presentation/Views/RecordEditorWindow.xaml`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/Assets/Localization/ru-RU.json`
- `Taskloom/Assets/Localization/en-US.json`
- `Taskloom/Assets/Themes/WarmLightTheme.xaml`
- `Taskloom/Assets/Themes/NeutralLightTheme.xaml`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Статус события и напоминание являются частью сущности события, поэтому должны проходить через домен, persistence и UI, а не быть временным состоянием экрана.
- `NotifyIcon.ShowBalloonTip` выбран как MVP-реализация Windows-уведомления без добавления тяжёлой Windows App SDK-инфраструктуры.

## 2026-04-23 - Запоминание размера и состояния главного окна

### Что сделано

- `AppSettings` расширен параметрами placement главного окна: наличие сохранённого состояния, ширина, высота и состояние окна.
- При первом запуске без сохранённых параметров главное окно открывается в состоянии `Maximized`.
- При следующих запусках сохранённый размер применяется и окно центрируется на рабочей области экрана.
- При закрытии сохраняется последнее состояние окна: `Normal` или `Maximized`.
- Координаты окна намеренно не сохраняются, чтобы приложение не открылось вне экрана после смены конфигурации мониторов.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/Services/Settings/AppSettings.cs`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/Presentation/Views/MainWindow.xaml.cs`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Размер и состояние окна относятся к пользовательским настройкам приложения, поэтому должны храниться в JSON-настройках, а не в SQLite.
- Центрирование восстановленного размера надёжнее сохранения координат при изменении количества или расположения мониторов.

## 2026-04-23 - Исправление зависания при закрытии приложения

### Что сделано

- Убрано синхронное ожидание `AppSettingsService.LoadAsync()` и `SaveAsync()` из `MainWindow.OnClosing`.
- Закрытие окна переведено на безопасный async-сценарий: первое закрытие отменяется, placement сохраняется, затем окно закрывается повторно.
- Добавлен флаг, исключающий повторное сохранение placement при повторном вызове `Close()`.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/Presentation/Views/MainWindow.xaml.cs`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- `GetAwaiter().GetResult()` на async file I/O в WPF UI-потоке может вызвать deadlock и полное зависание приложения.
- Закрытие окна должно сохранять настройки без блокировки message pump.

## 2026-04-23 - Склейка календаря и навигационных кнопок

### Что сделано

- Левая календарная панель переработана: календарь и кнопки `Предыдущий день` / `Следующий день` помещены в один центральный прозрачный блок.
- Кнопки больше не прибиты к нижнему краю внешней панели и визуально идут сразу под календарём.
- Убрана третья строка layout, из-за которой навигация уезжала слишком низко на `Maximized`.
- Сборка `dotnet build .\Taskloom.sln` выполнена без ошибок; предупреждения связаны с тем, что запущенный процесс `Taskloom` держал `Taskloom.exe`.

### Изменённые и созданные файлы

- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Календарь и навигация по дням являются одним рабочим блоком и не должны визуально разъезжаться по высоте панели.
- Внутренние контейнеры должны оставаться прозрачными, чтобы не создавать эффект контейнера внутри контейнера.

## 2026-04-23 - Устранение белой подложки стандартного календаря

### Что сделано

- `Calendar` в главном окне перестал быть прозрачным и теперь явно окрашен через `App.SurfaceSecondaryBrush`.
- Для внутренних элементов `CalendarItem`, `CalendarButton` и `CalendarDayButton` добавлены theme-aware стили.
- Дополнительно внутри `Calendar.Resources` переопределены системные кисти `SystemColors.WindowBrushKey`, `ControlBrushKey`, `HighlightBrushKey` и связанные ресурсы, потому что стандартный шаблон WPF продолжал брать белый фон из системной темы.
- После проверки стало ясно, что стандартный `CalendarItem` рисует белую подложку глубже, поэтому добавлен локальный `ControlTemplate` для `CalendarItem` с theme-aware фоном.
- После повторной проверки добавлен локальный `CalendarPanelStyle`, который переопределяет template самого `Calendar` и убирает внешний стандартный border.
- Белая системная подложка стандартного WPF `Calendar` заменена фоном текущей темы.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Простая прозрачность не убирала внутренний фон шаблона `CalendarItem`.
- Локальный template надёжнее, чем попытка маскировать белую область внешним контейнером или системными brush-ресурсами.

## 2026-04-23 - Добавление тёмных тем

### Что сделано

- Добавлена тема `SoftDarkTheme.xaml`: светло-серая тёмная схема со светлыми текстовыми кистями.
- Добавлена тема `DeepDarkTheme.xaml`: тёмно-серая схема со светлыми текстовыми кистями.
- `ThemeIds` расширен идентификаторами `soft-dark` и `deep-dark`.
- `ThemeService` подключает новые темы в список доступных схем.
- Добавлены русские и английские названия новых тем.
- Для новых тем задан полный набор ресурсов `App.*Brush`, включая status/chip palettes.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/Assets/Themes/SoftDarkTheme.xaml`
- `Taskloom/Assets/Themes/DeepDarkTheme.xaml`
- `Taskloom/Infrastructure/Theming/ThemeIds.cs`
- `Taskloom/Infrastructure/Theming/ThemeService.cs`
- `Taskloom/Assets/Localization/ru-RU.json`
- `Taskloom/Assets/Localization/en-US.json`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Тёмные темы реализованы через существующий theme layer без новой инфраструктуры.
- Полный набор кистей нужен, чтобы карточки, chips, статусы, календарь и окна не выпадали в системные цвета.

## 2026-04-23 - Светло-серая тема и переименование тёмных тем

### Что сделано

- Добавлена светло-серая светлая тема `GrayLightTheme.xaml` с тёмными текстовыми кистями.
- Добавлен идентификатор `ThemeIds.GrayLight`.
- `ThemeService` подключает новую тему в список доступных схем.
- Тема `SoftDark` переименована для пользователя в `Графитовая`.
- Тема `DeepDark` переименована для пользователя в `Глубокая тёмная`.
- Добавлены английские названия `Gray light`, `Graphite`, `Deep dark`.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/Assets/Themes/GrayLightTheme.xaml`
- `Taskloom/Infrastructure/Theming/ThemeIds.cs`
- `Taskloom/Infrastructure/Theming/ThemeService.cs`
- `Taskloom/Assets/Localization/ru-RU.json`
- `Taskloom/Assets/Localization/en-US.json`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Пользовательское название темы должно описывать реальный визуальный характер.
- Светлая серая тема должна быть отдельной light-схемой, а не переименованием тёмной темы.

## 2026-04-23 - Исправление читаемости тёмных тем

### Что сделано

- Добавлен глобальный theme-aware template для `ComboBoxItem`, чтобы выпадающие списки не использовали светлый системный popup с нечитаемым светлым текстом.
- У календаря удалены локальные системные кисти, жёстко заданные под светлую тему.
- Для `CalendarButton` и `CalendarDayButton` добавлены локальные templates с hover, selected и today состояниями.
- В тёмных темах усилен контраст `SurfaceMuted`, `SurfaceHover`, `SurfaceSelected`, `BorderBrush` и `BorderStrongBrush`.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/App.xaml`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/Assets/Themes/SoftDarkTheme.xaml`
- `Taskloom/Assets/Themes/DeepDarkTheme.xaml`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Стандартные WPF popup-элементы не наследуют тему приложения полностью, поэтому для тёмных тем им нужен явный template.
- Контраст кнопок в тёмных темах должен быть выше, чем в светлых, иначе вторичные кнопки сливаются с фоном панели.

## 2026-04-23 - Исправление закрытого состояния ComboBox и чисел календаря

### Что сделано

- Добавлен глобальный theme-aware template для закрытого состояния `ComboBox`.
- Выбранное значение `ComboBox` теперь рисуется через `App.TextPrimaryBrush`, а не системным цветом.
- Popup `ComboBox` продолжает использовать theme brushes и общий `ComboBoxItem` template.
- В templates календарных кнопок добавлен явный `TextElement.Foreground` для чисел.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/App.xaml`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- В WPF закрытое состояние `ComboBox` и выпадающие элементы стилизуются разными частями шаблона.
- Числа календаря должны получать foreground из темы явно, иначе в тёмной теме может остаться системный тёмный цвет.

## 2026-04-23 - Исправление поля даты в тёмных темах

### Что сделано

- Для `DatePicker` добавлены theme-aware `Foreground`, `Background` и `BorderBrush`.
- Для внутреннего `DatePickerTextBox` добавлены theme-aware `Foreground`, `Background`, `BorderBrush` и `SelectionBrush`.
- Для `DatePickerTextBox` добавлен собственный `ControlTemplate`, чтобы убрать белую рамку стандартного шаблона.
- Для `DatePicker` добавлен собственный template с фиксированной кнопкой открытия календаря.
- Поле даты в редакторе ограничено шириной 220px, чтобы оно не растягивалось на всю форму.
- Popup-календарь `DatePicker` переведён на theme-aware templates для `Calendar` и `CalendarItem`.
- Popup-календарь `DatePicker` привязан через `CalendarStyle` и обязательную часть `PART_Calendar`, чтобы WPF не подставлял стандартный белый календарь.
- Ширина поля даты увеличена до 260px.
- Внешний border popup-календаря `DatePicker` сделан прозрачным, чтобы убрать белую рамку вокруг календаря.
- Исправлена передача `CalendarItemStyle` в `PART_CalendarItem` внутри `ThemedPopupCalendarStyle`; без этого DatePicker использовал стандартный белый `CalendarItem`.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/App.xaml`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- `DatePicker` использует внутренний `DatePickerTextBox`, который не исправляется стилями `ComboBox`.
- Поля ввода даты должны явно брать цвета из текущей темы.

## 2026-04-23 - Нейтрализация акцентов тёмных тем

### Что сделано

- В тёмных темах primary, selected, task и info акценты переведены из сине-голубой гаммы в нейтрально-серую.
- Обновлены `AccentPrimary`, `AccentPrimaryHover`, `AccentPrimaryPressed`, `BorderSelected`, `SelectionStripe`, `AccentTask`, `StatusInfo*`.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/Assets/Themes/SoftDarkTheme.xaml`
- `Taskloom/Assets/Themes/DeepDarkTheme.xaml`
- `Taskloom/App.xaml`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Сине-голубые акценты визуально конфликтовали с графитовой тёмной темой.
- Нейтрально-серый акцент лучше поддерживает характер тёмной темы и не перетягивает внимание.

## 2026-04-23 - Исправление читаемости чисел календаря

### Что сделано

- В основном календаре `CalendarButton` и `CalendarDayButton` заменили `ContentPresenter` на явный `TextBlock`.
- В popup-календаре `DatePicker` добавлены templates для `CalendarButton` и `CalendarDayButton` с явным `TextBlock`.
- Текст календарных кнопок теперь берёт `Foreground` через `TemplateBinding`, а не через системный presenter.
- Для главного календаря цвет чисел задан напрямую через `App.TextPrimaryBrush`, потому что `TemplateBinding Foreground` не изменил визуальное состояние стандартных `CalendarDayButton`.
- Для главного календаря стили `CalendarButton` и `CalendarDayButton` вынесены в ресурсы окна и явно подключены через свойства `CalendarButtonStyle` и `CalendarDayButtonStyle`.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/App.xaml`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- `ContentPresenter` внутри шаблона календарных кнопок не гарантировал применение theme foreground к сгенерированному тексту.
- Для тёмных тем цифры календаря должны явно наследовать `App.TextPrimaryBrush` через свойство `Foreground`.

## 2026-04-23 - Custom title bar вспомогательных окон

### Что сделано

- У окна настроек убрана стандартная системная шапка.
- У окна редактора записи убрана стандартная системная шапка.
- Для обоих вспомогательных окон добавлена кастомная верхняя область в стиле приложения.
- Сохранено перетаскивание окон за кастомную верхнюю область.
- В редакторе записи сохранена возможность разворачивания/восстановления двойным кликом по кастомной шапке.
- Из настроек убрана внутренняя кнопка `Закрыть`; закрытие выполняется крестиком в кастомной шапке.
- Кнопка `Применить` в настройках теперь только сохраняет и применяет язык/тему, не закрывая окно.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые файлы

- `Taskloom/Presentation/Views/SettingsWindow.xaml`
- `Taskloom/Presentation/Views/SettingsWindow.xaml.cs`
- `Taskloom/Presentation/ViewModels/SettingsViewModel.cs`
- `Taskloom/Presentation/Views/RecordEditorWindow.xaml`
- `Taskloom/Presentation/Views/RecordEditorWindow.xaml.cs`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Вспомогательные окна должны визуально совпадать с главным окном и текущими темами.
- `Применить` в настройках не должно закрывать окно, потому что это действие сохранения, а не навигации.

## 2026-04-23 - Исправление параметров редактора записи

### Что сделано

- Блок `Параметры задачи` теперь скрывается целиком, если выбран не тип `Задача`.
- Для `CheckBox` выполнения задачи явно задан theme-aware цвет текста.
- Убрано локальное скрытие только самого `CheckBox`, из-за которого в событии оставался пустой блок параметров задачи.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые файлы

- `Taskloom/Presentation/Views/RecordEditorWindow.xaml`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Условная видимость параметров типа должна применяться к контейнеру всего блока, иначе редактор показывает пустые и нерелевантные секции.
- Текст интерактивных элементов должен брать цвет из текущей темы.

## 2026-04-23 - Контрастная оболочка вспомогательных окон

### Что сделано

- Для окна настроек добавлена внешняя контрастная рамка на `App.BorderStrongBrush`.
- Для окна редактора записи добавлена внешняя контрастная рамка на `App.BorderStrongBrush`.
- Тело вспомогательных окон переведено на `App.SurfacePrimaryBrush`.
- Кастомная шапка вспомогательных окон переведена на `App.SurfaceSecondaryBrush`.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые файлы

- `Taskloom/Presentation/Views/SettingsWindow.xaml`
- `Taskloom/Presentation/Views/RecordEditorWindow.xaml`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- После удаления системной рамки вспомогательные окна визуально сливались с фоном приложения.
- Контраст должен задаваться через существующий theme layer, а не через жёсткий цвет, чтобы решение работало во всех темах.

## 2026-04-23 - Нормальный размер редактора записи

### Что сделано

- Стартовый размер `RecordEditorWindow` увеличен до более подходящего для длинной формы.
- Минимальный размер редактора увеличен, чтобы окно не открывалось в слишком сжатом состоянии.
- Высота редактора ограничивается рабочей областью экрана при инициализации окна.
- Нижняя панель с кнопками `Отмена` и `Сохранить` вынесена из прокручиваемой области.
- Сообщение валидации перенесено в закреплённую нижнюю панель над кнопками.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые файлы

- `Taskloom/Presentation/Views/RecordEditorWindow.xaml`
- `Taskloom/Presentation/Views/RecordEditorWindow.xaml.cs`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Длинная форма события не должна открываться в размере, где критичные кнопки сразу оказываются за скроллом.
- При нехватке места прокручиваться должна основная форма, а действия сохранения и отмены должны оставаться доступными.

## 2026-04-23 - Добавление стилизационных тем ОС

### Что сделано

- Добавлена тема `Windows11Theme.xaml` с чистой светлой Mica-like палитрой и синим акцентом.
- Добавлена тема `UbuntuTheme.xaml` с тёмной aubergine-палитрой и оранжевым акцентом.
- Добавлена тема `Windows7Theme.xaml` со светлой Aero-like голубой палитрой.
- Добавлена тема `MacOsTheme.xaml` со светлой aqua/graphite палитрой.
- Добавлены идентификаторы новых тем в `ThemeIds`.
- Новые темы подключены в `ThemeService`.
- Добавлены русские и английские названия новых тем.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/Assets/Themes/Windows11Theme.xaml`
- `Taskloom/Assets/Themes/UbuntuTheme.xaml`
- `Taskloom/Assets/Themes/Windows7Theme.xaml`
- `Taskloom/Assets/Themes/MacOsTheme.xaml`
- `Taskloom/Infrastructure/Theming/ThemeIds.cs`
- `Taskloom/Infrastructure/Theming/ThemeService.cs`
- `Taskloom/Assets/Localization/ru-RU.json`
- `Taskloom/Assets/Localization/en-US.json`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Новые темы реализованы через существующий WPF `ResourceDictionary` layer, без новых UI-абстракций.
- Темы являются аккуратными цветовыми стилизациями под узнаваемые палитры ОС, а не попыткой копировать системные контролы.

## 2026-04-23 - Коррекция maximized-границ безрамочных окон

### Что сделано

- Добавлен `WindowMaximizeBoundsHelper` для обработки `WM_GETMINMAXINFO`.
- Главное окно подключает helper, чтобы `Maximized` ограничивался рабочей областью монитора.
- Окно редактора записи подключает helper, потому что оно тоже поддерживает разворачивание.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/Common/Windowing/WindowMaximizeBoundsHelper.cs`
- `Taskloom/Presentation/Views/MainWindow.xaml.cs`
- `Taskloom/Presentation/Views/RecordEditorWindow.xaml.cs`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- У безрамочного WPF-окна стандартное разворачивание может использовать полный размер монитора и заходить под панель задач.
- Обработка `WM_GETMINMAXINFO` задаёт `MaxPosition` и `MaxSize` по рабочей области конкретного монитора.

## 2026-04-23 - Добавление авторских визуальных тем

### Что сделано

- Добавлена тема `FrogGreenTheme.xaml`: салатово-зелёная палитра с болотным frog vibe.
- Добавлена тема `VolcanicFireTheme.xaml`: тёмная вулканическая палитра с рыжими, жёлтыми и красными lava-акцентами.
- Добавлена тема `CosmicColdTheme.xaml`: холодная космическая палитра с чёрным, тёмно-фиолетовым, сиреневым и белыми star-like акцентами.
- Добавлена тема `SnowWhiteTheme.xaml`: почти белая soft low-contrast палитра с едва голубоватым холодным оттенком.
- Добавлены идентификаторы новых тем в `ThemeIds`.
- Новые темы подключены в `ThemeService`.
- Добавлены русские и английские названия новых тем.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/Assets/Themes/FrogGreenTheme.xaml`
- `Taskloom/Assets/Themes/VolcanicFireTheme.xaml`
- `Taskloom/Assets/Themes/CosmicColdTheme.xaml`
- `Taskloom/Assets/Themes/SnowWhiteTheme.xaml`
- `Taskloom/Infrastructure/Theming/ThemeIds.cs`
- `Taskloom/Infrastructure/Theming/ThemeService.cs`
- `Taskloom/Assets/Localization/ru-RU.json`
- `Taskloom/Assets/Localization/en-US.json`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Новые визуальные направления реализованы через существующий theme layer и не требуют новых UI-абстракций.
- Каждая тема задаёт полный набор базовых, акцентных и semantic-кистей, чтобы интерфейс не выпадал в цвета другой темы.

## 2026-04-23 - Точный выбор времени события

### Что сделано

- Выбор времени начала и окончания события заменён с одного `ComboBox` интервалов на три селектора: часы, минуты, секунды.
- Добавлен `TimePartOptionViewModel` для форматированного отображения значений `00-59` и `00-23`.
- Удалён `TimeOptionViewModel`, потому что список 15-минутных интервалов больше не используется.
- `RecordEditorViewModel` синхронизирует выбранные части времени с внутренними `StartTime` и `EndTime` типа `TimeOnly?`.
- Для нового события при переключении типа задаётся безопасное время по умолчанию: `09:00:00` - `10:00:00`.
- Убрана причина отображения сырого типизированного значения в закрытом поле `ComboBox`.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/Presentation/ViewModels/RecordEditorViewModel.cs`
- `Taskloom/Presentation/ViewModels/TimePartOptionViewModel.cs`
- `Taskloom/Presentation/ViewModels/TimeOptionViewModel.cs`
- `Taskloom/Presentation/Views/RecordEditorWindow.xaml`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Событие должно поддерживать точный выбор времени, а не только грубые 15-минутные интервалы.
- UI не должен показывать пользователю внутреннее представление `TimeOnly`/nullable-значения.

## 2026-04-23 - Исправление отображения селекторов времени

### Что сделано

- Для `TimePartOptionViewModel` добавлено переопределение `ToString()`, возвращающее форматированный `Title`.
- Закрытое состояние `ComboBox` селектора времени теперь должно показывать `00`, `09`, `30`, а не имя класса option-модели.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые файлы

- `Taskloom/Presentation/ViewModels/TimePartOptionViewModel.cs`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Текущий theme-aware template `ComboBox` в закрытом состоянии может получать сам объект выбранного элемента.
- Option-модель должна иметь безопасное строковое представление для UI, чтобы не показывать техническое имя типа.

## 2026-04-23 - Подготовка уведомлений к Windows 11 notification flow

### Что сделано

- Добавлен интерфейс `IAppNotificationService` для транспорта локальных уведомлений.
- Добавлена временная реализация `WindowsBalloonAppNotificationService` поверх tray balloon.
- `WindowsBalloonEventReminderService` больше не владеет `NotifyIcon` напрямую.
- Для событий добавлено отдельное уведомление `Событие началось`.
- Логика напоминания больше не показывает просроченное напоминание задним числом: уведомление срабатывает только в коротком окне после целевого времени.
- Частота проверки напоминаний увеличена до 15 секунд.
- Добавлены локализационные строки для уведомления о начале события.
- В план добавлен этап Windows App SDK notifications, tray mode, task reminders и временной иконки.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/Services/Notifications/IAppNotificationService.cs`
- `Taskloom/Infrastructure/Notifications/WindowsBalloonAppNotificationService.cs`
- `Taskloom/Infrastructure/Notifications/WindowsBalloonEventReminderService.cs`
- `Taskloom/App.xaml.cs`
- `Taskloom/Assets/Localization/ru-RU.json`
- `Taskloom/Assets/Localization/en-US.json`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Windows 11 notification center требует нормального app notification transport, а tray balloon остаётся временным fallback.
- Событию нужны два разных пользовательских сигнала: предварительное напоминание и факт начала.
- Планировщик уведомлений не должен быть связан с конкретным UI-транспортом.

## 2026-04-23 - Восстановление сборки после прерванного Windows App SDK restore

### Что сделано

- Незавершённое подключение `Microsoft.WindowsAppSDK` откатили из `Taskloom.csproj`.
- Временная реализация `WindowsAppSdkNotificationService` удалена.
- `App` снова использует временный `WindowsBalloonAppNotificationService`.
- Выполнен `dotnet restore .\Taskloom.sln` для пересоздания `project.assets.json` под `net10.0-windows`.
- Сборка `dotnet build .\Taskloom.sln --no-restore` выполнена успешно без предупреждений и ошибок.

### Изменённые файлы

- `Taskloom/Taskloom.csproj`
- `Taskloom/App.xaml.cs`
- `Taskloom/Infrastructure/Notifications/WindowsAppSdkNotificationService.cs`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Прерванный restore оставил `obj/project.assets.json` под временно изменённый TargetFramework, из-за чего IDE и сборка показывали массовые ошибки.
- Подключение Windows App SDK нужно делать отдельным контролируемым шагом, чтобы не оставлять проект в полусобранном состоянии.

## 2026-04-23 - Tray mode и временная иконка

### Что сделано

- Создана временная иконка приложения `Assets/Icons/Taskloom.ico`.
- Иконка подключена как `ApplicationIcon` в `Taskloom.csproj`.
- Иконка подключена к главному окну, окну настроек и редактору записи.
- Добавлен `WindowsTrayService`, который владеет единственным `NotifyIcon`.
- Добавлено tray menu: `Открыть`, `Настройки`, `Выход`.
- Двойной клик по tray icon восстанавливает главное окно.
- Крестик главного окна теперь скрывает приложение в трей вместо завершения процесса.
- Реальное завершение приложения выполняется через `Выход` в tray menu.
- `WindowsBalloonAppNotificationService` использует общий tray icon и больше не создаёт отдельный `NotifyIcon`.
- Сборка `dotnet build .\Taskloom.sln --no-restore` выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/Assets/Icons/Taskloom.ico`
- `Taskloom/Infrastructure/Tray/WindowsTrayService.cs`
- `Taskloom/Infrastructure/Notifications/WindowsBalloonAppNotificationService.cs`
- `Taskloom/App.xaml.cs`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/Presentation/Views/MainWindow.xaml.cs`
- `Taskloom/Presentation/Views/SettingsWindow.xaml`
- `Taskloom/Presentation/Views/RecordEditorWindow.xaml`
- `Taskloom/Taskloom.csproj`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Taskloom должен оставаться активным в фоне для напоминаний, поэтому закрытие окна не должно автоматически завершать приложение.
- Tray icon должен быть единым местом управления фоновым режимом и временными balloon-уведомлениями.

## 2026-04-23 - Windows App SDK notifications

### Что сделано

- Подключён пакет `Microsoft.WindowsAppSDK`.
- Target framework проекта уточнён до `net10.0-windows10.0.19041.0`.
- Для unpackaged WPF-приложения добавлены `WindowsPackageType=None` и runtime identifiers.
- Добавлен `WindowsAppSdkNotificationService` на базе `AppNotificationManager`.
- Уведомления теперь отправляются через Windows App SDK и должны сохраняться в центре уведомлений Windows 11.
- `WindowsBalloonAppNotificationService` оставлен как fallback, если Windows App SDK notifications недоступны в текущей среде.
- Клик по уведомлению восстанавливает главное окно через существующий tray/open flow.
- Сборка `dotnet build .\Taskloom.sln` через SDK 10 из Rider выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/Taskloom.csproj`
- `Taskloom/App.xaml.cs`
- `Taskloom/Infrastructure/Notifications/WindowsAppSdkNotificationService.cs`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/Decisions.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Tray balloon был временной fallback-логикой и не закрывал задачу сохранения уведомлений в центре уведомлений Windows.
- Транспорт уведомлений уже был вынесен за `IAppNotificationService`, поэтому замена реализации не потребовала изменений в домене, ViewModels или планировщике событий.

## 2026-04-23 - Напоминания задач

### Что сделано

- В `TaskRecord` добавлено собственное nullable-время напоминания `ReminderTime`.
- В `CalendarRecordDraft` и `CalendarRecordDataModel` добавлено отдельное поле времени напоминания задачи.
- SQLite schema расширена колонкой `task_reminder_time`; инициализатор БД добавляет её и для существующей базы.
- Репозиторий и прикладной сервис сохраняют и восстанавливают task reminder отдельно от event reminder.
- Редактор записи получил включаемое время напоминания в блоке `Параметры задачи`.
- Добавлены локализационные строки для UI и текста уведомления о задаче.
- Планировщик уведомлений теперь проверяет pending-задачи с включённым временем напоминания и отправляет для них отдельное уведомление.
- Сборка `dotnet build .\Taskloom.sln` выполнена успешно без предупреждений и ошибок.

### Изменённые файлы

- `Taskloom/Domain/TaskRecord.cs`
- `Taskloom/Services/Records/CalendarRecordDraft.cs`
- `Taskloom/Data/Models/CalendarRecordDataModel.cs`
- `Taskloom/Data/Sql/CreateSchema.sql`
- `Taskloom/Infrastructure/Storage/SqliteDatabaseInitializer.cs`
- `Taskloom/Infrastructure/Repositories/SqliteCalendarRecordRepository.cs`
- `Taskloom/Services/Records/CalendarRecordService.cs`
- `Taskloom/Services/Notifications/IEventReminderService.cs`
- `Taskloom/Infrastructure/Notifications/WindowsBalloonEventReminderService.cs`
- `Taskloom/Presentation/ViewModels/RecordEditorViewModel.cs`
- `Taskloom/Presentation/Views/RecordEditorWindow.xaml`
- `Taskloom/Assets/Localization/ru-RU.json`
- `Taskloom/Assets/Localization/en-US.json`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- План проекта прямо требовал добавить task reminders через отдельные поля domain/data/schema/editor, а не переиспользовать event-only модель.
- Для задачи без собственного времени события минимальная согласованная модель — отдельное nullable-время напоминания на дату задачи.

## 2026-04-23 - Документирование внешних зависимостей запуска

### Что сделано

- В документации зафиксировано, что проект собирается под `net10.0-windows10.0.19041.0`.
- Зафиксировано требование установленного .NET 10 Desktop Runtime для запуска приложения.
- Зафиксировано требование Windows App Runtime 1.8 для Windows notifications через `AppNotificationManager`.
- Отдельно отмечено, что при отсутствии или неполной установке Windows App Runtime приложение использует fallback-уведомления через tray balloon.
- Зафиксировано, что текущий дистрибутив Taskloom не вшивает Windows App Runtime внутрь себя и рассматривает его как внешнюю зависимость среды.

### Изменённые файлы

- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Пользователь не должен выяснять требования запуска по журналу ошибок или из кода.
- Для Windows App SDK notifications критично явно фиксировать внешние runtime-зависимости, иначе воспроизводимость установки и запуска остаётся непредсказуемой.

## 2026-04-23 - Multiple images для записей

### Что сделано

- Для всех типов записей добавлена загрузка нескольких изображений через `Обзор` в редакторе.
- В domain добавлены `RecordAttachment` и `RecordAttachmentKind`; запись теперь может содержать attachments как часть агрегата.
- В SQLite schema используется отдельная таблица `record_attachments`; для порядка изображений добавлено поле `sort_order`.
- Добавлено локальное файловое хранилище `RecordImageStorageService`, которое копирует `jpg/jpeg/png/bmp/gif` в `%LocalAppData%\Taskloom\Media\Images`.
- В редакторе записи показывается grid миниатюр; изображения можно удалять и переставлять влево/вправо.
- В карточке записи на главном экране показывается grid миниатюр до четырёх изображений; конкретная миниатюра открывает свой файл системным просмотрщиком через shell.
- При удалении или замене состава вложений локальные лишние файлы удаляются после успешного сохранения; при удалении записи удаляются все локальные файлы записи.
- Сборка `dotnet build .\Taskloom.sln` через SDK 10 выполнена успешно без предупреждений и ошибок.

### Изменённые файлы

- `Taskloom/Domain/CalendarRecord.cs`
- `Taskloom/Domain/RecordAttachment.cs`
- `Taskloom/Domain/RecordAttachmentKind.cs`
- `Taskloom/Services/Records/CalendarRecordDraft.cs`
- `Taskloom/Services/Records/RecordImageDraft.cs`
- `Taskloom/Services/Records/IRecordImageStorageService.cs`
- `Taskloom/Services/Records/CalendarRecordService.cs`
- `Taskloom/Data/Models/RecordAttachmentDataModel.cs`
- `Taskloom/Data/Sql/CreateSchema.sql`
- `Taskloom/Infrastructure/Storage/TaskloomPaths.cs`
- `Taskloom/Infrastructure/Storage/RecordImageStorageService.cs`
- `Taskloom/Infrastructure/Storage/SqliteDatabaseInitializer.cs`
- `Taskloom/Infrastructure/Repositories/SqliteCalendarRecordRepository.cs`
- `Taskloom/Presentation/ViewModels/MainWindowViewModel.cs`
- `Taskloom/Presentation/ViewModels/RecordImageListItemViewModel.cs`
- `Taskloom/Presentation/ViewModels/RecordEditorViewModel.cs`
- `Taskloom/Presentation/ViewModels/RecordListItemViewModel.cs`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/Presentation/Views/RecordEditorWindow.xaml`
- `Taskloom/Presentation/Views/RecordEditorWindow.xaml.cs`
- `Taskloom/Common/Converters/ImagePathToBitmapConverter.cs`
- `Taskloom/Assets/Localization/ru-RU.json`
- `Taskloom/Assets/Localization/en-US.json`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Хранить только внешний путь к пользовательскому файлу нельзя: вложение станет невалидным при переносе или удалении исходника.
- Отдельная таблица attachments с `sort_order` лучше масштабируется, чем nullable image-поля внутри `calendar_records`, и позволяет дойти до `strip mode` без переделки storage-модели.

## 2026-04-23 - Кликабельные ссылки в описании записи

### Что сделано

- Добавлен `LinkParser`, который разбивает текст описания на обычные фрагменты и ссылки.
- Поддержаны URI со схемой (`http`, `https`, `ftp`, `tg`, `mailto`, `file` и другие схемы), домены без схемы, email и IPv4-адреса.
- Для доменов без схемы используется `https://`, для IPv4 без схемы — `http://`, для email — `mailto:`.
- Добавлен `TextBlockLinkBehavior`, который рендерит `Run/Hyperlink` поверх исходного текста `Details`.
- Открытие ссылки идёт через системный обработчик `Process.Start(..., UseShellExecute = true)`.
- Исходный текст записи не меняется и продолжает храниться в `Details` без нормализации или перезаписи.
- Сборка `dotnet build .\Taskloom.sln` через SDK 10 выполнена успешно без предупреждений и ошибок.

### Изменённые файлы

- `Taskloom/Common/Text/LinkToken.cs`
- `Taskloom/Common/Text/LinkParser.cs`
- `Taskloom/Common/Text/TextBlockLinkBehavior.cs`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Ссылки должны быть presentation-поведением, а не трансформацией данных: запись хранит исходный текст как есть.
- Attached behavior для `TextBlock` даёт минимально инвазивный способ включить кликабельные ссылки без перехода на rich text editor или markup-хранение.

## 2026-04-23 - Audio attachments и playlist в карточке записи

### Что сделано

- Для `record_attachments` добавлены audio-метаданные: `display_title`, `duration_seconds`, `preview_relative_path`.
- Добавлен `RecordAudioStorageService`, который импортирует `mp3/wav/m4a/flac/wma/ogg`, читает title/duration/cover через `TagLibSharp` и сохраняет файлы в `%LocalAppData%\Taskloom\Media\Audio` и `%LocalAppData%\Taskloom\Media\AudioCovers`.
- `CalendarRecordService` расширен поддержкой нескольких audio attachments на запись, включая очистку удалённых локальных файлов и обложек.
- Редактор записи умеет добавлять несколько аудиофайлов, показывает title, имя файла, длительность, обложку и позволяет менять порядок треков.
- Главное окно показывает playlist прямо в карточке записи: cover, play/pause, seek, длительность и отдельное открытие аудиофайла через shell.
- Для воспроизведения добавлен единый `AudioPlaybackService` на базе WPF `MediaPlayer`; одновременно воспроизводится только один трек.
- Сборка `dotnet build .\Taskloom.sln` через SDK 10 выполнена успешно без предупреждений и ошибок.

### Изменённые и созданные файлы

- `Taskloom/Taskloom.csproj`
- `Taskloom/App.xaml.cs`
- `Taskloom/Common/Converters/AudioCoverSourceToBitmapConverter.cs`
- `Taskloom/Data/Models/RecordAttachmentDataModel.cs`
- `Taskloom/Data/Sql/CreateSchema.sql`
- `Taskloom/Domain/CalendarRecord.cs`
- `Taskloom/Domain/RecordAttachment.cs`
- `Taskloom/Domain/RecordAttachmentKind.cs`
- `Taskloom/Infrastructure/Media/AudioPlaybackService.cs`
- `Taskloom/Infrastructure/Repositories/SqliteCalendarRecordRepository.cs`
- `Taskloom/Infrastructure/Storage/RecordAudioStorageService.cs`
- `Taskloom/Infrastructure/Storage/SqliteDatabaseInitializer.cs`
- `Taskloom/Infrastructure/Storage/TaskloomPaths.cs`
- `Taskloom/Presentation/ViewModels/MainWindowViewModel.cs`
- `Taskloom/Presentation/ViewModels/RecordAudioListItemViewModel.cs`
- `Taskloom/Presentation/ViewModels/RecordEditorViewModel.cs`
- `Taskloom/Presentation/ViewModels/RecordListItemViewModel.cs`
- `Taskloom/Presentation/Views/MainWindow.xaml`
- `Taskloom/Presentation/Views/MainWindow.xaml.cs`
- `Taskloom/Presentation/Views/RecordEditorWindow.xaml`
- `Taskloom/Presentation/Views/RecordEditorWindow.xaml.cs`
- `Taskloom/Services/Media/AudioPlaybackStateChangedEventArgs.cs`
- `Taskloom/Services/Media/IAudioPlaybackService.cs`
- `Taskloom/Services/Records/CalendarRecordDraft.cs`
- `Taskloom/Services/Records/CalendarRecordService.cs`
- `Taskloom/Services/Records/IRecordAudioStorageService.cs`
- `Taskloom/Services/Records/RecordAudioDraft.cs`
- `Taskloom/Assets/Localization/ru-RU.json`
- `Taskloom/Assets/Localization/en-US.json`
- `Taskloom/docs/ProjectOverview.md`
- `Taskloom/docs/Architecture.md`
- `Taskloom/docs/ContinuationGuide.md`
- `Taskloom/docs/DevelopmentPlan.md`
- `Taskloom/docs/WorkLog.md`

### Обоснование

- Audio playlist лучше строить поверх уже существующей attachment-модели, чем заводить отдельную сущность только для музыки.
- Единый playback service нужен, чтобы карточки записей не запускали несколько конкурирующих плееров одновременно.
- Метаданные аудио нужно читать при импорте и сохранять рядом с attachment, иначе список записей начнёт лезть в файлы при каждом открытии дня.
