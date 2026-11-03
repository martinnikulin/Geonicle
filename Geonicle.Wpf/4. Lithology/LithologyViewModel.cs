using Data.Models;
using DevExpress.Data.Filtering;
using DevExpress.Mvvm;
using Geonicle.Data.Services;
using Geonicle.Infrastructure;
using Geonicle.Validation;
using Geonicle.Wpf.Menus;
using Geonicle.Wpf.Ribbon;
using Geonicle.Wpf.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Geonicle.Wpf.ViewModels
{
    /// <summary>
    /// Экран «Данные по скважинам»: пластопересечения выбранной скважины и прослоя (сверху)
    /// и литология скважины (внизу). Скважину и прослой задаёт лента через DataFilter.
    /// </summary>
    public class LithologyViewModel : DataViewModelBase
    {
        private readonly IHoleDataService dataService;
        private readonly DataFilter filter;
        private readonly IUserDialogs dialogs;
        private (int? BoreholeId, int? InterbedId) loadedKey;
        private bool isLoading;
        private bool lookupsLoaded;

        public LithologyViewModel(IHoleDataService dataService, DataFilter filter, IUserDialogs dialogs)
            : base(dialogs)
        {
            this.dataService = dataService;
            this.filter = filter;
            this.dialogs = dialogs;

            // Контекстные меню гридов экрана: пункты и команды объявляются здесь.
            AddHoleSeamCommand = new AsyncCommand(AddHoleSeamAsync, () => CanAddHoleSeam);
            RedefineHoleSeamCommand = new AsyncCommand(RedefineHoleSeamAsync, () => CanRedefineHoleSeam);
            HoleSeamsMenu = CreateGridMenu();
            SamplesMenu = CreateGridMenu();
            LithologyMenu = CreateGridMenu()
                .Add("Добавить пластопересечение", AddHoleSeamCommand)
                .Add("Переопределить пластопересечение", RedefineHoleSeamCommand);

            filter.Changed += OnFilterChanged;
            Pending = Task.CompletedTask;
        }

        public override string Title => "Данные по скважинам";

        // Экран «Данные по скважинам»: общая группа, фильтр по прослою и фильтр по линии и скважине.
        public override RibbonGroups VisibleRibbonGroups =>
            RibbonGroups.Data | RibbonGroups.Interbed | RibbonGroups.LineBorehole;

        /// <summary>Последняя запущенная сменой фильтра перезагрузка (чтобы тесты могли дождаться её окончания).</summary>
        public Task Pending { get; private set; }

        public GridMenu HoleSeamsMenu { get; }

        public GridMenu LithologyMenu { get; }

        public GridMenu SamplesMenu { get; }

        /// <summary>Добавить пластопересечение по выбранным строкам литологии (пункт меню грида литологии).</summary>
        public AsyncCommand AddHoleSeamCommand { get; }

        /// <summary>Изменить границы выбранного пластопересечения по выбранным строкам литологии (пункт меню грида литологии).</summary>
        public AsyncCommand RedefineHoleSeamCommand { get; }

        /// <summary>
        /// Выбранные в гриде литологии строки и его сортировка; задаёт грид (LithologyGridSelection) и читается в момент вызова.
        /// </summary>
        public Func<LayerSelection>? LayerSelectionProvider { get; set; }

        /// <summary>
        /// Пункты меню доступны, если грид показывает всю литологию (флажок снят), выбраны строки и грид отсортирован по глубине.
        /// </summary>
        public bool CanAddHoleSeam => CanEditBoundaries();

        public bool CanRedefineHoleSeam => CanEditBoundaries() && SelectedHoleSeam != null;

        /// <summary>Допустимые значения колонки «КБ».</summary>
        public IReadOnlyList<string> KbValues => HoleSeamKb.Values;

        public ObservableCollection<SeamDef>? HoleSeams
        {
            get => GetValue<ObservableCollection<SeamDef>?>();
            private set => SetValue(value);
        }

        public ObservableCollection<Lithology>? Lithology
        {
            get => GetValue<ObservableCollection<Lithology>?>();
            private set => SetValue(value);
        }

        /// <summary>Пробы скважины (грид «Пробы» на вкладке «Анализы»).</summary>
        public ObservableCollection<SampleRow>? Samples
        {
            get => GetValue<ObservableCollection<SampleRow>?>();
            private set => SetValue(value);
        }

        public SeamDef? SelectedHoleSeam
        {
            get => GetValue<SeamDef?>();
            set
            {
                SetValue(value);
                UpdateLithologyFilter();
                UpdateSampleView();
                RaiseSeamCommandsChanged();
            }
        }

        /// <summary>«Показать литологию и анализы по пластопересечениям»: только слои внутри выбранного пластопересечения.</summary>
        public bool ShowByHoleSeam
        {
            get => GetValue<bool>();
            set
            {
                SetValue(value);
                UpdateLithologyFilter();
                UpdateSampleView();
                RaiseSeamCommandsChanged();
                RaisePropertyChanged(nameof(AcceptedReadOnly));
                RaisePropertyChanged(nameof(CanAddOrDeleteSamples));
            }
        }

        /// <summary>«Принято» (HSSamps.Accepted) правится только в режиме пластопересечения; иначе колонка только для чтения.</summary>
        public bool AcceptedReadOnly => !ShowByHoleSeam;

        /// <summary>Пробы добавляются и удаляются только в режиме всей скважины (в режиме пластопересечения кнопки недоступны).</summary>
        public bool CanAddOrDeleteSamples => !ShowByHoleSeam;

        /// <summary>Условие для грида проб: null — все пробы скважины, иначе только пробы выбранного пластопересечения.</summary>
        public CriteriaOperator? SamplesFilterCriteria
        {
            get => GetValue<CriteriaOperator?>();
            private set => SetValue(value);
        }

        /// <summary>Условие для грида литологии: null — все слои скважины.</summary>
        public CriteriaOperator? LithologyFilterCriteria
        {
            get => GetValue<CriteriaOperator?>();
            private set => SetValue(value);
        }

        public IReadOnlyList<LookupItem>? SeamLookup
        {
            get => GetValue<IReadOnlyList<LookupItem>?>();
            private set => SetValue(value);
        }

        public IReadOnlyList<LookupItem>? CoalGradeLookup
        {
            get => GetValue<IReadOnlyList<LookupItem>?>();
            private set => SetValue(value);
        }

        public IReadOnlyList<LookupItem>? CoalTypeLookup
        {
            get => GetValue<IReadOnlyList<LookupItem>?>();
            private set => SetValue(value);
        }

        public IReadOnlyList<LookupItem>? FormationLookup
        {
            get => GetValue<IReadOnlyList<LookupItem>?>();
            private set => SetValue(value);
        }

        /// <summary>Список «Притято по» (код — Dictionary.N, раздел «Ad»).</summary>
        public IReadOnlyList<LookupItem>? AdIndexLookup
        {
            get => GetValue<IReadOnlyList<LookupItem>?>();
            private set => SetValue(value);
        }

        public IReadOnlyList<LookupItem>? RockLookup
        {
            get => GetValue<IReadOnlyList<LookupItem>?>();
            private set => SetValue(value);
        }

        protected override async Task LoadCoreAsync()
        {
            isLoading = true;
            try
            {
                // Пока шла загрузка, лента могла выбрать другую скважину или прослой: тогда читаем заново.
                (int? BoreholeId, int? InterbedId) key;
                do
                {
                    key = CurrentKey;
                    var previousSeamId = SelectedHoleSeam?.HoleSeamId;
                    var data = await dataService.LoadAsync(key.BoreholeId, key.InterbedId);
                    HoleSeams = data.HoleSeams;
                    Lithology = data.Lithology;
                    Samples = data.Samples;
                    SelectedHoleSeam = data.HoleSeams.FirstOrDefault(s => s.HoleSeamId == previousSeamId)
                        ?? data.HoleSeams.FirstOrDefault();
                    loadedKey = key;

                    // Справочники грузятся внутри цикла: пока идёт их запрос, лента может выбрать другую скважину.
                    if (!lookupsLoaded)
                    {
                        SeamLookup = await dataService.GetSeamLookupAsync();
                        CoalGradeLookup = await dataService.GetCoalGradeLookupAsync();
                        CoalTypeLookup = await dataService.GetCoalTypeLookupAsync();
                        FormationLookup = await dataService.GetFormationLookupAsync();
                        RockLookup = await dataService.GetRockLookupAsync();
                        AdIndexLookup = await dataService.GetAdIndexLookupAsync();
                        lookupsLoaded = true;
                    }
                }
                while (key != CurrentKey);
            }
            finally
            {
                isLoading = false;
            }
        }

        protected override async Task SaveCoreAsync()
        {
            await dataService.SaveAsync();
            dataService.UpdateSeamNames();   // колонка «Пласт» зависит от глубин, которые могли измениться
            await dataService.RefreshSamplesAsync();   // триггеры БД меняют связи проб, значения Ad берутся из представления
            UpdateSampleView();
        }

        protected override bool HasPendingChanges() => dataService.HasChanges;

        public override void Dispose()
        {
            filter.Changed -= OnFilterChanged;
            dataService.Dispose();
        }

        public Task AddHoleSeamAsync() =>
            EditBoundariesAsync(CanAddHoleSeam, async layers => SelectedHoleSeam = await dataService.AddHoleSeamAsync(layers));

        public Task RedefineHoleSeamAsync() =>
            EditBoundariesAsync(CanRedefineHoleSeam, layers => dataService.RedefineHoleSeamAsync(SelectedHoleSeam!, layers));

        /// <summary>Выполняет действие над границами по выбранным строкам; ошибки сервиса показывает пользователю.</summary>
        private async Task EditBoundariesAsync(bool allowed, Func<IReadOnlyList<Lithology>, Task> action)
        {
            var selection = LayerSelectionProvider?.Invoke();
            if (!allowed || selection == null)
                return;

            try
            {
                await action(selection.Layers);
            }
            catch (HoleDataException ex)
            {
                dialogs.Show(ex.Message, "Пластопересечение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Сущности об изменениях не уведомляют: гриды перечитывают значения (границы, колонка «Пласт»).
            RequestGridRefresh();
        }

        private bool CanEditBoundaries()
        {
            var selection = LayerSelectionProvider?.Invoke();
            return !ShowByHoleSeam
                && loadedKey.BoreholeId != null
                && loadedKey.InterbedId != null
                && selection is { SortedByDepth: true, Layers.Count: > 0 };
        }

        private void RaiseSeamCommandsChanged()
        {
            AddHoleSeamCommand.RaiseCanExecuteChanged();
            RedefineHoleSeamCommand.RaiseCanExecuteChanged();
        }

        private (int? BoreholeId, int? InterbedId) CurrentKey => (filter.BoreholeId, filter.InterbedId);

        /// <summary>Привязывает пробы к выбранному пластопересечению, нумерует их и задаёт условие показа проб.</summary>
        private void UpdateSampleView()
        {
            dataService.UpdateSampleView(SelectedHoleSeam?.HoleSeamId, ShowByHoleSeam);

            SamplesFilterCriteria = !ShowByHoleSeam
                ? null
                : SelectedHoleSeam != null
                    ? new BinaryOperator("HasSeamLink", true)
                    : new BinaryOperator("SampleId", -1);   // пластопересечения нет: проб не показываем
        }

        private void UpdateLithologyFilter()
        {
            if (!ShowByHoleSeam)
            {
                LithologyFilterCriteria = null;
                return;
            }

            // Слои между глубинами выбранного пластопересечения; если пластопересечения нет, не показываем ни одного.
            LithologyFilterCriteria = SelectedHoleSeam is { DepthFrom: { } from, DepthTo: { } to }
                ? new GroupOperator(
                    GroupOperatorType.And,
                    new BinaryOperator("DepthFrom", from, BinaryOperatorType.GreaterOrEqual),
                    new BinaryOperator("DepthTo", to, BinaryOperatorType.LessOrEqual))
                : new BinaryOperator("LithologyId", -1);
        }

        /// <summary>Лента выбрала другую скважину или прослой: перечитываем данные экрана.</summary>
        private void OnFilterChanged(object? sender, EventArgs e)
        {
            // Пока экран не загружен или грузится, актуальное значение подхватит сама загрузка.
            if (!IsInitialized || isLoading || CurrentKey == loadedKey)
                return;

            Pending = ReloadAfterFilterChangeAsync();
        }

        private async Task ReloadAfterFilterChangeAsync()
        {
            try
            {
                if (HasChanges)
                {
                    var answer = dialogs.Show(
                        "Есть несохранённые изменения. Сохранить их перед сменой скважины или прослоя?\n\n«Нет» — отбросить изменения.",
                        "Несохранённые изменения",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (answer == MessageBoxResult.Yes)
                        await SaveAsync();
                }

                await LoadAsync();
            }
            catch (DataValidationException ex)
            {
                dialogs.Show(ex.ToUserMessage(), "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                dialogs.Show(ex.InnerException?.Message ?? ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
