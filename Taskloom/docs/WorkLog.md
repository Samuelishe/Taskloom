# Журнал работ

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
