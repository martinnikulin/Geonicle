# Geonicle — соглашения проекта

Файл читается в начале каждой сессии. Новое решение, о котором договорились, записываем сюда, чтобы оно действовало для всех будущих экранов и гридов.

## Окружение и работа
- Стартовый проект решения — `Geonicle.Wpf`: задан в `Geonicle.slnx` атрибутом `DefaultStartup="true"` (действует при первом открытии решения; дальше Visual Studio хранит выбор в локальных файлах `.vs/`). Новые проекты в решение добавляем без этого атрибута.
- Общее правило работы: сначала предложение, затем ожидание согласия пользователя, и только потом изменения кода и файлов. Вопросы и чтение кода согласования не требуют.
- WPF + DevExpress + EF Core, .NET 10. Клиент собирается и запускается только на Windows; на Linux-сервере собирается лишь `Geonicle.Data`. Изменения в WPF/XAML на сервере непроверены, пользователь проверяет их на Windows и присылает текст ошибок.
- Правки идут в `master`, небольшими коммитами; после каждой правки — push и сообщение, где изменения.
- Репозиторий публичный: никаких паролей, строк подключения с секретами и схем/данных БД.

## База данных
- Схема БД в `gdb/` (скрипты пользователя) — источник истины. При расхождении меняем код приложения; миграции и DDL не пишем и не предлагаем в репозитории.

## Архитектура
- MVVM только на `DevExpress.Mvvm` (`ViewModelBase`, `GetValue/SetValue`, `AsyncCommand`). `CommunityToolkit.Mvvm` и генераторы кода не используем.
- DI: контейнер собирается в `App.xaml.cs`; модели экранов и сервисы регистрируются там, `MainViewModel` создаёт экраны из контейнера.
- Строка подключения — `appsettings.json` (`ConnectionStrings:GDB52`). Контексты создаются через `IDbContextFactory<GDB52Context>` (`GDB52ContextFactory`), `AddDbContextFactory` не использовать.
- Данные экрана: сервис наследует `EditableDataService` (владеет контекстом на время жизни экрана); модель экрана наследует `DataViewModelBase` (Load/Refresh/Save, `HasChanges`).
- Диалоги из моделей — только через `IUserDialogs`. Имя `IDialogService` занято в DevExpress.Mvvm, не использовать.
- При уходе с экрана с несохранёнными изменениями спрашиваем (см. `MainViewModel.NavigateAsync`). Закрытие окна пока не обрабатываем.

## Новый экран: чек-лист
1. Сервис данных от `EditableDataService`, интерфейс от `IEditableDataService`; регистрация в `App.xaml.cs`.
2. Модель от `DataViewModelBase`; контекстные меню гридов объявляются в конструкторе (см. ниже).
3. В `MainWindow.xaml`: `DataTemplate` для модели и пункт `AccordionItem` с `Tag`.
4. Каждый грид — отдельный `UserControl` в `Views/Grids/` со своими колонками, настроенный по списку «Стандартные настройки гридов». Экран (`Views/XxxView.xaml`) только размещает гриды.
5. Загрузка при открытии — через `InitializeCommand` по событию `Loaded` (как в `BoreholesView.xaml`).

## Стандартные настройки гридов
Каждый грид — отдельный `UserControl` в `Views/Grids/` (образец: `BoreholesGrid.xaml`, `SurveysGrid.xaml`). Колонки описываются явно в этом файле: `AutoGenerateColumns="None"` и `GridColumn` с `FieldName` и `Header`. Подписи колонок и скрытие полей в сгенерированных классах EF (`[DisplayName]`, `[Browsable]`) не храним: скаффолд их стирает. DataContext грид наследует от экрана.

Каждый `GridControl` и его `TableView`:

`GridControl`:
- `AutoGenerateColumns="None"` и явный список `GridControl.Columns` (первичный ключ-счётчик — `ReadOnly="True"`)
- `RestoreStateKeyFieldName="<первичный ключ сущности>"` и `RestoreStateOnSourceChange="True"` — чтобы при обновлении текущая строка и прокрутка сохранялись
- `TotalSummary` со счётчиком строк (`GridSummaryItem SummaryType="Count"`, `Alignment="Left"`)

`TableView`:
- `IncrementalSearchMode="Enabled"`, `UseOnlyCurrentColumnInIncrementalSearch="True"`, `IncrementalSearchClearDelay="1500"` — поиск по текущей колонке при вводе, как в Delphi
- `EditorShowMode="MouseDownFocused"` (см. журнал решений: не менять без проверки вместе с поиском)
- `DataNavigatorButtons="Navigation, AddNewRow, DeleteFocusedRow"`, `ShowDataNavigator="True"`, `ShowGroupPanel="False"`, `AutoWidth="True"`
- `menus:GridContextMenu.Menu="{Binding <МенюГрида>}"`

### Выпадающие списки в колонках
- Справочник для `ComboBoxEditSettings` модель экрана загружает отдельным лёгким запросом (`AsNoTracking`, только код и название, см. `BoreholeLookupItem`) и отдаёт свойством.
- В XAML грида список берём через `helpers:BindingProxy` (ресурс `Data="{Binding}"`), потому что `EditSettings` не видят DataContext. Образец: `Views/Grids/LineHolesGrid.xaml`.

- Колонки с `enum`: явный `ComboBoxEditSettings` с `ItemsSource="{dxe:EnumItemsSource EnumType={x:Type models:Имя}}"` и `IsTextEditable="False"` (подписи берутся из `[Display(Name=...)]`). Автоматически редактор-список для enum при явных колонках не создаётся.

### Контекстные меню
- Меню каждого грида — отдельное свойство `GridMenu` в модели экрана, создаётся в конструкторе через `CreateGridMenu()` (в нём уже есть «Обновить»); свои пункты: `.Add("Подпись", команда)`.
- В XAML файла грида только `menus:GridContextMenu.Menu="{Binding ...}"` на `TableView`; код-behind для меню не нужен.

## Данные и EF
- Ручные настройки модели EF — в `Geonicle.Data/GDB52Context.Partial.cs` (`OnModelCreatingPartial`), сгенерированный `GDB52Context.cs` не правим. Обязательные связи `ClientSetNull` переводятся в `Cascade` (только в трекере EF): иначе удаление строки подчинённого грида из коллекции родителя падает с «association has been severed».

- У `Coeffs` в модели приложения ключ — `CoeffId` (в БД составной): иначе EF не даёт менять в гриде прослой, тип и марку у существующей строки. Это в `GDB52Context.Partial.cs`.
- Справочники для списков в колонках — `LookupItem(Id, Name)` (`ValueMember="Id" DisplayMember="Name"`), по одному свойству на справочник в модели экрана.

## Логика БД и код
- Триггеры и хранимые процедуры остаются в БД; перехвата `SaveChanges` в коде нет (ни собственного, ни `EntityFrameworkCore.Triggered`: пробовали и отказались). В код переносим только то, что пользователь явно убирает из БД. Реестр — `docs/db-logic-migration.md`, обновляем вместе с кодом.
- Перенесено: нулевой замер у новой скважины. Как в Delphi-версии, он появляется в гриде сразу при создании строки, до сохранения (`BoreholesService.Boreholes_CollectionChanged`). Для этого все зависимые коллекции в моделях — `ObservableCollection` (`public virtual ICollection<T> X { get; set; } = new ObservableCollection<T>();`, как в XAF), поэтому код может добавлять подчинённые строки, а связанный грид их сразу видит. Новые модели пишем так же.
- `DateCreated`/`DateModified` всегда проставляет сервер БД, в коде их не пишем. DDL делает пользователь.

## Проверка вводимых данных
- Правила проверки — FluentValidation (пакет в `Geonicle.Data`), папка `Geonicle.Data/Validation/`, один класс на сущность (`<Сущность>Validator : AbstractValidator<T>`), сообщения по-русски прямо в правилах (`WithMessage`), ограничения согласуем со схемой БД. Образец: `BoreholeValidator`.
- FluentValidation проверяет только объекты и ничего не знает о гриде. Связка с гридом (`ValidateRow`), проверка при сохранении и регистрация в DI пока не сделаны; делаем по отдельному решению.

## Раскладка экранов
- Размещение и разделение гридов на всех экранах — через `dxlc:LayoutControl`. Подпись грида — `dxlc:LayoutGroup Header="..." View="GroupBox"` (подпись сверху слева, рамка); `View="Group"` заголовок не показывает. У верхних групп `dxlc:LayoutControl.AllowVerticalSizing="True"`.

## Журнал решений
- Привязки к команде главной модели из popup-меню грида ненадёжны, поэтому `GridContextMenu` передаёт команды объектами.
- Инкрементальный поиск: пробовали `AllowIncrementalSearch` на колонках и `EditorShowMode="MouseUpFocused"` — отказались, оставили настройки `TableView` из списка выше.
- Колонки гридов: автогенерация не используем, описание колонок каждого грида в отдельном файле `Views/Grids/<Имя>Grid.xaml` (решение пользователя).
