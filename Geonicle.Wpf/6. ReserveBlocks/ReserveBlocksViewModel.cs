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
    /// Экран «Блоки подсчёта запасов»: блоки выбранной версии, варианта и детали пласта; для выбранного блока — его части
    /// и скважины. Пласт и прослой задаёт лента через DataFilter; версия, вариант и деталь — списки группы ленты «Блок»,
    /// их данные принадлежат этой модели (строки варианта правятся и сохраняются вместе с экраном).
    /// </summary>
    public class ReserveBlocksViewModel : DataViewModelBase
    {
        private const int DefaultVersionId = 1;

        private readonly IReserveBlocksService dataService;
        private readonly DataFilter filter;
        private readonly IUserDialogs dialogs;
        private (int? SeamId, int? InterbedId) loadedKey;
        private bool isLoading;
        private bool lookupsLoaded;
        private int blocksVersion;

        public ReserveBlocksViewModel(IReserveBlocksService dataService, DataFilter filter, IUserDialogs dialogs)
            : base(dialogs)
        {
            this.dataService = dataService;
            this.filter = filter;
            this.dialogs = dialogs;

            // Контекстные меню гридов экрана: пункты и команды объявляются здесь.
            BlocksMenu = CreateGridMenu();
            DetailsMenu = CreateGridMenu();
            UnitsMenu = CreateGridMenu();

            filter.Changed += OnFilterChanged;
            Pending = Task.CompletedTask;
        }

        public override string Title => "Блоки подсчёта запасов";

        // Экран «Блоки подсчёта запасов»: общая группа, пласт, прослой и группа «Блок» (версия, вариант, деталь).
        public override RibbonGroups VisibleRibbonGroups =>
            RibbonGroups.Data | RibbonGroups.Seam | RibbonGroups.Interbed | RibbonGroups.Block;

        /// <summary>Последняя запущенная выбором в ленте или сменой фильтра загрузка (чтобы тесты могли дождаться её окончания).</summary>
        public Task Pending { get; private set; }

        public GridMenu BlocksMenu { get; }

        public GridMenu DetailsMenu { get; }

        public GridMenu UnitsMenu { get; }

        // ---- группа ленты «Блок»: списки и выбранные значения

        public IReadOnlyList<LookupItem> Versions
        {
            get => GetValue<IReadOnlyList<LookupItem>?>() ?? Array.Empty<LookupItem>();
            private set => SetValue(value);
        }

        public int? SelectedVersionId
        {
            get => GetValue<int?>();
            set
            {
                SetValue(value);
                OnSelectionChanged();
            }
        }

        /// <summary>Варианты пласта и прослоя (строки SeamConds; «Ссылка на вариант» правится в выпадающем гриде ленты).</summary>
        public ObservableCollection<VariantRow> Variants
        {
            get => GetValue<ObservableCollection<VariantRow>?>() ?? new ObservableCollection<VariantRow>();
            private set => SetValue(value);
        }

        public int? SelectedVariantId
        {
            get => GetValue<int?>();
            set
            {
                SetValue(value);
                OnSelectionChanged();
            }
        }

        /// <summary>Кондиции прослоя для колонки «Ссылка на вариант».</summary>
        public IReadOnlyList<LookupItem> ConditionLookup
        {
            get => GetValue<IReadOnlyList<LookupItem>?>() ?? Array.Empty<LookupItem>();
            private set => SetValue(value);
        }

        public IReadOnlyList<LookupItem> Parts
        {
            get => GetValue<IReadOnlyList<LookupItem>?>() ?? Array.Empty<LookupItem>();
            private set => SetValue(value);
        }

        public int? SelectedPartId
        {
            get => GetValue<int?>();
            set
            {
                SetValue(value);
                OnSelectionChanged();
            }
        }

        // ---- области экрана

        public ObservableCollection<Block>? Blocks
        {
            get => GetValue<ObservableCollection<Block>?>();
            private set => SetValue(value);
        }

        /// <summary>Выбранный блок: его части и скважины показываются нижними гридами, комментарий — полем под гридом блоков.</summary>
        public Block? SelectedBlock
        {
            get => GetValue<Block?>();
            set => SetValue(value);
        }

        /// <summary>Тексты пластопересечений для списка «Скважина»: «скважина, комментарий».</summary>
        public IReadOnlyList<string> HoleTexts
        {
            get => GetValue<IReadOnlyList<string>?>() ?? Array.Empty<string>();
            private set => SetValue(value);
        }

        public IReadOnlyList<LookupItem>? CoalTypeLookup
        {
            get => GetValue<IReadOnlyList<LookupItem>?>();
            private set => SetValue(value);
        }

        public IReadOnlyList<LookupItem>? CoalGradeLookup
        {
            get => GetValue<IReadOnlyList<LookupItem>?>();
            private set => SetValue(value);
        }

        public IReadOnlyList<LookupItem>? BoundaryLookup
        {
            get => GetValue<IReadOnlyList<LookupItem>?>();
            private set => SetValue(value);
        }

        public IReadOnlyList<LookupItem>? PillarLookup
        {
            get => GetValue<IReadOnlyList<LookupItem>?>();
            private set => SetValue(value);
        }

        /// <summary>Выбор версии, варианта или детали в ленте: показываем блоки (правки ранее показанных блоков остаются).</summary>
        private void OnSelectionChanged()
        {
            if (!isLoading && IsInitialized)
                Pending = ShowBlocksAsync();
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
                    var seamChanged = key.SeamId != loadedKey.SeamId;
                    var lists = await dataService.LoadListsAsync(key.SeamId, key.InterbedId);

                    if (!lookupsLoaded)
                    {
                        Versions = await dataService.GetVersionsAsync();
                        CoalTypeLookup = await dataService.GetCoalTypeLookupAsync();
                        CoalGradeLookup = await dataService.GetCoalGradeLookupAsync();
                        BoundaryLookup = await dataService.GetBoundaryLookupAsync();
                        PillarLookup = await dataService.GetPillarLookupAsync();
                        lookupsLoaded = true;
                    }

                    Variants = lists.Variants;
                    ConditionLookup = lists.ConditionLookup;
                    Parts = lists.Parts;
                    HoleTexts = lists.Holes.Select(h => h.Text).ToList();

                    // Версия: прежняя, а при первой загрузке запись 1. Вариант: прежний, если остался в списке, иначе первый.
                    // Деталь: при смене пласта всегда первая, при смене прослоя прежняя (если осталась в списке).
                    SetValue(KeepOrFirst(SelectedVersionId ?? DefaultVersionId, Versions.Select(v => v.Id)), nameof(SelectedVersionId));
                    SetValue(KeepOrFirst(SelectedVariantId, lists.Variants.Select(v => v.ConditionId)), nameof(SelectedVariantId));
                    SetValue(KeepOrFirst(seamChanged ? null : SelectedPartId, lists.Parts.Select(p => p.Id)), nameof(SelectedPartId));

                    await ShowBlocksCoreAsync(++blocksVersion);
                    loadedKey = key;
                }
                while (key != CurrentKey);
            }
            finally
            {
                isLoading = false;
            }
        }

        private static int? KeepOrFirst(int? current, IEnumerable<int> keys)
        {
            var list = keys.ToList();
            if (current != null && list.Contains(current.Value))
                return current;
            return list.Count > 0 ? list[0] : (int?)null;
        }

        protected override Task SaveCoreAsync() => dataService.SaveAsync();

        protected override bool HasPendingChanges() => dataService.HasChanges;

        public override void Dispose()
        {
            filter.Changed -= OnFilterChanged;
            dataService.Dispose();
        }

        /// <summary>Показывает блоки выбранных версии, варианта и детали (устаревший ответ отбрасывается).</summary>
        private async Task ShowBlocksAsync()
        {
            var version = ++blocksVersion;
            try
            {
                await ShowBlocksCoreAsync(version);
            }
            catch (Exception ex)
            {
                dialogs.Show(ex.InnerException?.Message ?? ex.Message, "Ошибка загрузки", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ShowBlocksCoreAsync(int version)
        {
            var blocks = await dataService.LoadBlocksAsync(SelectedVersionId, SelectedVariantId, SelectedPartId);
            if (version != blocksVersion)
                return;   // пока шла загрузка, выбрали другую версию, вариант или деталь

            var previousId = SelectedBlock?.BlockId;
            Blocks = blocks;
            SelectedBlock = blocks.FirstOrDefault(b => b.BlockId == previousId && previousId != 0) ?? blocks.FirstOrDefault();
        }

        /// <summary>
        /// В строке скважины блока выбрали или ввели название (код-behind грида): проставляются HoleSeamId и От/До,
        /// а для названия вне списка HoleSeamId = NULL.
        /// </summary>
        public void UnitHoleChanged(BlockUnit? unit)
        {
            if (unit == null)
                return;

            dataService.ApplyHole(unit);
            RequestGridRefresh();   // От и До пластопересечения — вычисляемые, сущности об изменениях не уведомляют
        }

        private (int? SeamId, int? InterbedId) CurrentKey => (filter.SeamId, filter.InterbedId);

        /// <summary>Лента выбрала другой пласт или прослой: перечитываем списки и блоки.</summary>
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
