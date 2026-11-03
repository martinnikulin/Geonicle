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
0. Папка экрана «N. Имя» (см. «Структура папок») в `Geonicle.Wpf` и в `Geonicle.Data`.
1. Сервис данных от `EditableDataService`, интерфейс от `IEditableDataService`; регистрация в `App.xaml.cs`.
2. Модель от `DataViewModelBase`; контекстные меню гридов объявляются в конструкторе (см. ниже).
3. В `MainWindow.xaml`: `DataTemplate` для модели и пункт `AccordionItem` с `Tag`.
4. Каждый грид — отдельный `UserControl` в папке экрана со своими колонками, настроенный по списку «Стандартные настройки гридов». Экран (`XxxView.xaml` в той же папке) только размещает гриды.
5. Загрузка при открытии — через `InitializeCommand` по событию `Loaded` (как в `BoreholesView.xaml`).

## Стандартные настройки гридов
Каждый грид — отдельный `UserControl` в папке экрана (образец: `BoreholesGrid.xaml`, `SurveysGrid.xaml`). Колонки описываются явно в этом файле: `AutoGenerateColumns="None"` и `GridColumn` с `FieldName` и `Header`. Подписи колонок и скрытие полей в сгенерированных классах EF (`[DisplayName]`, `[Browsable]`) не храним: скаффолд их стирает. DataContext грид наследует от экрана.

Каждый `GridControl` и его `TableView`:

`GridControl`:
- `AutoGenerateColumns="None"` и явный список `GridControl.Columns` (первичный ключ-счётчик — `ReadOnly="True"`)
- `behaviors:GridRefresh.RefreshOnSave="True"` — после сохранения грид перечитывает значения строк (ID новых строк появляется сразу; сущности об изменениях не уведомляют)
- `RestoreStateKeyFieldName="<первичный ключ сущности>"` и `RestoreStateOnSourceChange="True"` — чтобы при обновлении текущая строка и прокрутка сохранялись
- `TotalSummary` со счётчиком строк (`GridSummaryItem SummaryType="Count"`, `Alignment="Left"`)

`TableView`:
- `IncrementalSearchMode="Enabled"`, `UseOnlyCurrentColumnInIncrementalSearch="True"`, `IncrementalSearchClearDelay="1500"` — поиск по текущей колонке при вводе, как в Delphi
- `EditorShowMode="MouseDownFocused"` (см. журнал решений: не менять без проверки вместе с поиском)
- `DataNavigatorButtons="Navigation, AddNewRow, DeleteFocusedRow"`, `ShowDataNavigator="True"`, `ShowGroupPanel="False"`, `AutoWidth="True"`
- `menus:GridContextMenu.Menu="{Binding <МенюГрида>}"`

### Выпадающие списки в колонках
- Справочник для `ComboBoxEditSettings` модель экрана загружает отдельным лёгким запросом (`AsNoTracking`, только код и название, см. `BoreholeLookupItem`) и отдаёт свойством.
- В XAML грида список берём через `helpers:BindingProxy` (ресурс `Data="{Binding}"`), потому что `EditSettings` не видят DataContext. Образец: `2. Lines/LineHolesGrid.xaml`.

- Колонки с `enum`: явный `ComboBoxEditSettings` с `ItemsSource="{dxe:EnumItemsSource EnumType={x:Type models:Имя}}"` и `IsTextEditable="False"` (подписи берутся из `[Display(Name=...)]`). Автоматически редактор-список для enum при явных колонках не создаётся.

### Контекстные меню
- Меню каждого грида — отдельное свойство `GridMenu` в модели экрана, создаётся в конструкторе через `CreateGridMenu()` (в нём уже есть «Обновить»); свои пункты: `.Add("Подпись", команда)`.
- В XAML файла грида только `menus:GridContextMenu.Menu="{Binding ...}"` на `TableView`; код-behind для меню не нужен.

## Данные и EF
- Ручные настройки модели EF — в `Geonicle.Data/GDB52Context_.cs` (`OnModelCreatingPartial`), сгенерированный `GDB52Context.cs` не правим. Обязательные связи `ClientSetNull` переводятся в `Cascade` (только в трекере EF): иначе удаление строки подчинённого грида из коллекции родителя падает с «association has been severed».

- Исключение из каскада: внешние ключи границ пластопересечения (`SeamDef.DepthFromId`, `DepthToId` → `Lithology`) имеют `NoAction`: иначе удаление слоя литологии в гриде тихо удалило бы загруженное пластопересечение (в БД каскада нет). Там же, в `GDB52Context_.cs`.
- Пластопересечения создаются и меняются из грида литологии (как в Delphi): выбрать строки (SelectionMode=Row), в контекстном меню «Добавить пластопересечение» или «Переопределить пластопересечение». Первая и последняя выбранные строки задают `DepthFromId` и `DepthToId`; новое пластопересечение получает `SeamId = 0` («пласт без названия», он всегда должен быть в БД) и стопки по глубинам границ. Пункты доступны, только если флажок «по пластопересечениям» снят, выбраны строки и грид отсортирован по глубине по возрастанию (`LithologyGridSelection` отдаёт модели выбор и сортировку в момент вызова). Логика — `HoleDataService.AddHoleSeamAsync/RedefineHoleSeamAsync`, ошибки для пользователя — `HoleDataException`. Удаление пластопересечения стандартное (БД каскадом удаляет связи с пробами и композиты). Пересечения пластопересечений одного прослоя и дубли границ проверяет `SeamDefValidator`.
- Правила удаления: `RowValidator.ValidateDelete` проверяется в `EntityValidation` для удаляемых строк; пласт с кодом 0 удалять нельзя (`SeamValidator`). Защиту на стороне БД делает пользователь.
- Если экран меняет строки в памяти (сущности об изменениях не уведомляют), он вызывает `RequestGridRefresh()`: гриды с `GridRefresh.RefreshOnSave` перечитывают значения.
- Колонка «Пласт» в гриде литологии — вычисляемое `Lithology.SeamName` (только чтение): название пласта того пластопересечения экрана, между глубинами которого лежит слой. `HoleDataService` заполняет его при загрузке и `UpdateSeamNames()` после сохранения.
- Глубины пластопересечения «От» и «До» — вычисляемые свойства `SeamDef.DepthFrom` и `DepthTo` (`4. Lithology/SeamDefDepths.cs`): берутся из слоёв литологии `DepthFromId` и `DepthToId`, как в представлении `HoleSeams`. Собственных столбцов в таблице `SeamDefs` нет (устаревшие убраны), сервис загружает навигации `DepthFromNavigation` и `DepthToNavigation`.
- У `Coeffs` в модели приложения ключ — `CoeffId` (в БД составной): иначе EF не даёт менять в гриде прослой, тип и марку у существующей строки. Это в `GDB52Context_.cs`.
- Справочники для списков в колонках — `LookupItem(Id, Name)` (`ValueMember="Id" DisplayMember="Name"`), по одному свойству на справочник в модели экрана.

## Логика БД и код
- Триггеры и хранимые процедуры остаются в БД; перехвата `SaveChanges` в коде нет (ни собственного, ни `EntityFrameworkCore.Triggered`: пробовали и отказались). В код переносим только то, что пользователь явно убирает из БД. Реестр — `docs/db-logic-migration.md`, обновляем вместе с кодом.
- Перенесено: нулевой замер у новой скважины. Как в Delphi-версии, он появляется в гриде сразу при создании строки, до сохранения (`BoreholesService.Boreholes_CollectionChanged`). Для этого все зависимые коллекции в моделях — `ObservableCollection` (`public virtual ICollection<T> X { get; set; } = new ObservableCollection<T>();`, как в XAF), поэтому код может добавлять подчинённые строки, а связанный грид их сразу видит. Новые модели пишем так же.
- `DateCreated`/`DateModified` всегда проставляет сервер БД, в коде их не пишем. DDL делает пользователь.

## Проверка вводимых данных
- Правила проверки — FluentValidation (пакет в `Geonicle.Data`), папка `Geonicle.Data/Validation/`, один класс на сущность (`<Сущность>Validator : AbstractValidator<T>`), сообщения по-русски прямо в правилах (`WithMessage`), ограничения согласуем со схемой БД. Образец: `BoreholeValidator`.
- Валидатор наследуем от `RowValidator<T>` (не напрямую от `AbstractValidator<T>`) и переопределяем `Describe` — как называть строку в сообщении («Скважина «A-1»»). Валидаторы находятся в сборке автоматически, регистрировать их не нужно.
- Проверка при сохранении: `EditableDataService.SaveAsync` вызывает `EntityValidation.Validate(context)` для добавленных и изменённых сущностей; при ошибках ничего не сохраняется, бросается `DataValidationException`, `MainViewModel` показывает список (до 10 ошибок, остальные счётчиком).
- FluentValidation проверяет только объекты и ничего не знает о гриде. Связка с гридом (`ValidateRow`, проверка при уходе со строки) и проверка в импорте из Excel пока не сделаны; делаем по отдельному решению.

## Лента (ribbon)
- Группы ленты, которые зависят от экрана, объявляются так: флаг в `Ribbon/RibbonGroups.cs`; экран сообщает нужные группы через `IRibbonContext.VisibleRibbonGroups` (у `DataViewModelBase` по умолчанию `Data`, экраны с фильтрами переопределяют); `MainViewModel` отдаёт `Is<Группа>Visible`; в `MainWindow.xaml` у `RibbonPageGroup` `IsVisible="{Binding Is<Группа>Visible}"`. Группа «Данные» (сохранить, обновить, добавить, удалить) видна на всех экранах с гридами.
- Фильтры на ленте (прослой, линия, скважина, «Все скважины»): `RibbonFilterViewModel` (доступна как `MainViewModel.Filters`) пишет выбор в общий `DataFilter` (одиночка в DI): экраны читают `InterbedId`, `LineId`, `BoreholeId`, `AllBoreholes` и подписываются на `Changed`. Выбор сохраняется при переключении экранов. Списки (`IFilterLookupService`) обновляются при переходе на экран, где они есть; сохраняется прежний выбор, если он остался в списке, иначе выбирается первый элемент; смена линии или «Все скважины» перезагружает скважины и выбирает первую.
- Экран, зависящий от фильтра (образец — `LithologyViewModel`): читает `DataFilter` при загрузке и подписывается на `Changed`; перезагружается, только если изменились его ключи (скважина и прослой), а не линия; если фильтр сменился во время загрузки, загрузка повторяется с новым значением; при несохранённых изменениях спрашивает «сохранить / отбросить» (при ошибке сохранения остаётся на прежних данных).
- Списки на ленте — `BarEditItem`; источник списка берётся через `helpers:BindingProxy` (`ViewModelProxy` в ресурсах окна). Все три списка шириной `EditWidth="120"`. Прослой — `ComboBoxEditSettings` (одна колонка, без заголовка), подпись «Разделяющий прослой» отдельным `BarStaticItem` над списком. Группа со списками линии и скважины называется «Скважина». Линия и скважины — `LookUpEditSettings` с `AutoPopulateColumns="False"` (иначе DevExpress подменяет свои колонки автоматическими, со всеми полями и английскими заголовками) и таблицей внутри (`PART_GridControl`, колонки «ID» и «Название» с заголовками, без N); порядок элементов задаёт сервис (по N), в закрытом поле показывается название.

## Тестирование
- Тесты данных: проект `Geonicle.Data.Tests` (xUnit, простые `Assert`, база InMemory; net10.0, запускается и на Linux, и на Windows). Запуск: `dotnet test Geonicle.Data.Tests`. Перед каждым коммитом изменений в `Geonicle.Data` тесты должны проходить.
- Автозапуск: GitHub Actions (`.github/workflows/tests.yml`) запускает `dotnet test Geonicle.Data.Tests` на Linux при каждом пуше в `master` и в pull request; результат на вкладке Actions репозитория. Красный запуск чиним сразу.
- Структура: `Validation/` (по файлу на валидатор + `EntityValidation`), `Services/` (сервисы данных: `BoreholesService` и др., зависимые коллекции), `Model/` (настройки модели EF, от которых зависит поведение). Фабрика контекстов для тестов — `TestDb` (своя база на каждый тест).
- Новый валидатор или триггер-подобная логика в `Geonicle.Data` сопровождается тестами в том же коммите: верные данные, каждое правило, описание строки.
- InMemory не проверяет уникальные индексы, внешние ключи, триггеры БД и SQL-специфику. Это проверяют интеграционные тесты на реальном SQL Server: `Geonicle.Data.Tests/Integration/` (`[SqlServerFact]`). Они выполняются только если задана переменная окружения `GEONICLE_TEST_CONNECTION` (строка подключения к тестовой БД; без неё пропускаются, в том числе на GitHub Actions). Каждый тест идёт внутри транзакции, которая откатывается, данные тестовой БД не меняются (счётчики identity продвигаются). Защита: имя базы в строке подключения должно содержать «test», иначе тесты откажутся запускаться. Запускает их пользователь на Windows (на сервере SQL Server нет), результат присылает. Все классы интеграционных тестов помечаем `[Collection("SqlServer")]`: тесты держат открытые транзакции, и при параллельном запуске разных классов SQL Server иногда давал взаимоблокировку (1205, жертвой становился случайный тест).
- Тесты WPF-моделей: проект `Geonicle.Wpf.Tests` (xUnit, net10.0-windows, только Windows, нужна лента DevExpress). `FakeDialogs` и `TestScreen` в `Fakes.cs`; `DataViewModelBaseTests`, `MainViewModelTests` и `CompositionTests` (контейнер собирается из `AppServices.AddGeonicleApp`, он же используется в `App`). Запуск: `dotnet test Geonicle.Wpf.Tests` на Windows. Правки в `Geonicle.Wpf` (модели, сервисы, регистрации) сопровождаем тестами там же. XAML и гриды юнит-тестами не покрываются, проверяются вручную.
- На сервере (Linux) логику моделей Wpf можно проверить временным проектом без DevExpress: исходники моделей и тестов подключаются как ссылки, `DevExpress.Mvvm.ViewModelBase`/`AsyncCommand` и перечисления `System.Windows.MessageBox*` заменяются заглушками; на net10.0-windows этот проект на Linux не запустить (нет WindowsDesktop).

## Структура папок
- Всё, что относится к одному экрану, лежит в одной папке с номером и именем: `1. Boreholes`, `2. Lines`, `3. Seams`, `4. Lithology`. Такая папка есть и в `Geonicle.Wpf` (вид, модель экрана, гриды), и в `Geonicle.Data` (сервис данных, валидаторы, вспомогательные классы справочников). Общее для всех экранов (`DataViewModelBase`, `EditableDataService`, меню, лента, `LookupItem`) остаётся в общих папках.
- Пространства имён по папкам экранов не меняем: `Geonicle.Wpf.Views`, `Geonicle.Wpf.Views.Grids`, `Geonicle.Wpf.ViewModels`, `Geonicle.Data.Services`, `Geonicle.Validation`.

## Раскладка экранов
- Размещение и разделение гридов на всех экранах — через `dxlc:LayoutControl`. Каждый грид помещается в группу-вкладку: `<dxlc:LayoutGroup View="Tabs"><dxlc:LayoutGroup Header="..."><grids:XxxGrid/></dxlc:LayoutGroup></dxlc:LayoutGroup>` (подпись выглядит как вкладка сверху слева, как в Delphi-версии). `GroupBox` не используем. Размеры меняются атрибутом `dxlc:LayoutControl.AllowVerticalSizing="True"` (или `AllowHorizontalSizing`) на внешней группе `View="Tabs"`.

## Журнал решений
- Привязки к команде главной модели из popup-меню грида ненадёжны, поэтому `GridContextMenu` передаёт команды объектами.
- Инкрементальный поиск: пробовали `AllowIncrementalSearch` на колонках и `EditorShowMode="MouseUpFocused"` — отказались, оставили настройки `TableView` из списка выше.
- Колонки гридов: автогенерация не используем, описание колонок каждого грида в отдельном файле `<Имя>Grid.xaml` в папке экрана (решение пользователя).
