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
