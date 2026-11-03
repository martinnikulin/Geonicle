using Data.Models;
using DevExpress.Data.Filtering;
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
            HoleSeamsMenu = CreateGridMenu();
            LithologyMenu = CreateGridMenu();

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

        public SeamDef? SelectedHoleSeam
        {
            get => GetValue<SeamDef?>();
            set
            {
                SetValue(value);
                UpdateLithologyFilter();
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
            }
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
                    SelectedHoleSeam = data.HoleSeams.FirstOrDefault(s => s.HoleSeamId == previousSeamId)
                        ?? data.HoleSeams.FirstOrDefault();
                    loadedKey = key;
                }
                while (key != CurrentKey);

                if (!lookupsLoaded)
                {
                    SeamLookup = await dataService.GetSeamLookupAsync();
                    CoalGradeLookup = await dataService.GetCoalGradeLookupAsync();
                    CoalTypeLookup = await dataService.GetCoalTypeLookupAsync();
                    FormationLookup = await dataService.GetFormationLookupAsync();
                    RockLookup = await dataService.GetRockLookupAsync();
                    lookupsLoaded = true;
                }
            }
            finally
            {
                isLoading = false;
            }
        }

        protected override Task SaveCoreAsync() => dataService.SaveAsync();

        protected override bool HasPendingChanges() => dataService.HasChanges;

        public override void Dispose()
        {
            filter.Changed -= OnFilterChanged;
            dataService.Dispose();
        }

        private (int? BoreholeId, int? InterbedId) CurrentKey => (filter.BoreholeId, filter.InterbedId);

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
