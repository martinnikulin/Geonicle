using Data.Models;
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
    /// Экран «Пластопересечения»: пластопересечения и средние выбранного пласта и прослоя (сверху), литология и пробы
    /// выбранного пластопересечения (снизу). Пласт и прослой задаёт лента через DataFilter.
    /// </summary>
    public class SeamIntersectionsViewModel : DataViewModelBase
    {
        private readonly ISeamIntersectionsService dataService;
        private readonly DataFilter filter;
        private readonly IUserDialogs dialogs;
        private readonly IAssaysWindowService assayWindows;
        private (int? SeamId, int? InterbedId) loadedKey;
        private bool isLoading;
        private bool lookupsLoaded;
        private int detailsVersion;

        public SeamIntersectionsViewModel(
            ISeamIntersectionsService dataService, DataFilter filter, IUserDialogs dialogs, IAssaysWindowService assayWindows)
            : base(dialogs)
        {
            this.dataService = dataService;
            this.filter = filter;
            this.dialogs = dialogs;
            this.assayWindows = assayWindows;

            // Контекстные меню гридов экрана: пункты и команды объявляются здесь.
            HoleSeamsMenu = CreateGridMenu();
            AveragesMenu = CreateGridMenu();
            LithologyMenu = CreateGridMenu();
            SamplesMenu = CreateGridMenu();

            filter.Changed += OnFilterChanged;
            Pending = Task.CompletedTask;
        }

        public override string Title => "Пластопересечения";

        // Экран «Пластопересечения»: общая группа, список пластов и список разделяющих прослоев.
        public override RibbonGroups VisibleRibbonGroups =>
            RibbonGroups.Data | RibbonGroups.Seam | RibbonGroups.Interbed;

        /// <summary>Последняя запущенная выбором строки или сменой фильтра загрузка (чтобы тесты могли дождаться её окончания).</summary>
        public Task Pending { get; private set; }

        public GridMenu HoleSeamsMenu { get; }

        public GridMenu AveragesMenu { get; }

        public GridMenu LithologyMenu { get; }

        public GridMenu SamplesMenu { get; }

        public ObservableCollection<SeamDef>? HoleSeams
        {
            get => GetValue<ObservableCollection<SeamDef>?>();
            private set => SetValue(value);
        }

        public ObservableCollection<Average>? Averages
        {
            get => GetValue<ObservableCollection<Average>?>();
            private set => SetValue(value);
        }

        /// <summary>Выбранное пластопересечение: по нему показываются литология и пробы.</summary>
        public SeamDef? SelectedHoleSeam
        {
            get => GetValue<SeamDef?>();
            set
            {
                SetValue(value);
                if (!isLoading)
                    Pending = LoadDetailsAsync(value);
            }
        }

        public ObservableCollection<Lithology>? Lithology
        {
            get => GetValue<ObservableCollection<Lithology>?>();
            private set => SetValue(value);
        }

        public ObservableCollection<SampleRow>? Samples
        {
            get => GetValue<ObservableCollection<SampleRow>?>();
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

        /// <summary>Показатели для колонки «Показатель» средних (AssayVars.Description).</summary>
        public IReadOnlyList<LookupItem>? AssayVarLookup
        {
            get => GetValue<IReadOnlyList<LookupItem>?>();
            private set => SetValue(value);
        }

        public IReadOnlyList<LookupItem>? RockLookup
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

        protected override async Task LoadCoreAsync()
        {
            isLoading = true;
            try
            {
                // Пока шла загрузка, лента могла выбрать другой пласт или прослой: тогда читаем заново.
                (int? SeamId, int? InterbedId) key;
                do
                {
                    key = CurrentKey;
                    var previousId = SelectedHoleSeam?.HoleSeamId;
                    var data = await dataService.LoadAsync(key.SeamId, key.InterbedId);
                    HoleSeams = data.HoleSeams;
                    Averages = data.Averages;
                    SelectedHoleSeam = data.HoleSeams.FirstOrDefault(s => s.HoleSeamId == previousId)
                        ?? data.HoleSeams.FirstOrDefault();
                    await ShowDetailsAsync(SelectedHoleSeam, ++detailsVersion);
                    loadedKey = key;

                    // Справочники грузятся внутри цикла: пока идёт их запрос, лента может выбрать другой пласт или прослой.
                    if (!lookupsLoaded)
                    {
                        CoalGradeLookup = await dataService.GetCoalGradeLookupAsync();
                        CoalTypeLookup = await dataService.GetCoalTypeLookupAsync();
                        AssayVarLookup = await dataService.GetAssayVarLookupAsync();
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
            await dataService.RefreshAfterSaveAsync();   // триггеры БД меняют связи проб, значения Ad берутся из представления
        }

        protected override bool HasPendingChanges() => dataService.HasChanges;

        public override void Dispose()
        {
            filter.Changed -= OnFilterChanged;
            assayWindows.CloseAll();   // окна анализов принадлежат экрану
            dataService.Dispose();
        }

        /// <summary>Выбрано другое пластопересечение: показываем его литологию и пробы (устаревший ответ отбрасывается).</summary>
        private async Task LoadDetailsAsync(SeamDef? seam)
        {
            var version = ++detailsVersion;
            try
            {
                await ShowDetailsAsync(seam, version);
            }
            catch (Exception ex)
            {
                dialogs.Show(ex.InnerException?.Message ?? ex.Message, "Ошибка загрузки", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ShowDetailsAsync(SeamDef? seam, int version)
        {
            var details = await dataService.LoadDetailsAsync(seam);
            if (version != detailsVersion)
                return;   // пока шла загрузка, выбрали другую строку

            Lithology = details.Lithology;
            Samples = details.Samples;
        }

        /// <summary>
        /// Открывает окно «Анализы» пробы (двойной щелчок по нередактируемой ячейке грида проб).
        /// Ввод анализов возможен только у сохранённой пробы; после записи анализов значения Ad в гриде проб перечитываются.
        /// </summary>
        public void OpenAssays(SampleRow? row)
        {
            if (row == null)
                return;

            if (row.SampleId == 0)
            {
                dialogs.Show("Сначала сохраните пробу: анализы вводятся только у сохранённой пробы.", "Анализы", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var title = string.IsNullOrWhiteSpace(row.Nsample) ? $"Анализы: проба ID {row.SampleId}" : $"Анализы: проба № {row.Nsample}";
            assayWindows.Open(row.SampleId, title, () => Pending = RefreshSampleValuesAsync());
        }

        private async Task RefreshSampleValuesAsync()
        {
            try
            {
                await dataService.RefreshSampleValuesAsync();
                RequestGridRefresh();   // значения Ad — из представления, сущности об изменениях не уведомляют
            }
            catch (Exception ex)
            {
                dialogs.Show(ex.InnerException?.Message ?? ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private (int? SeamId, int? InterbedId) CurrentKey => (filter.SeamId, filter.InterbedId);

        /// <summary>Лента выбрала другой пласт или прослой: перечитываем данные экрана.</summary>
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
                        "Есть несохранённые изменения. Сохранить их перед сменой пласта или прослоя?\n\n«Нет» — отбросить изменения.",
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
