using DevExpress.Mvvm;
using Geonicle.Infrastructure;
using Geonicle.Wpf.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Geonicle.Wpf.Ribbon
{
    /// <summary>
    /// Фильтры на ленте: «Пласт», «Разделяющий прослой», «Разведочная линия», «Скважины» и «Все скважины».
    /// Хранит списки и выбранные значения и передаёт выбор в общий DataFilter.
    /// </summary>
    public class RibbonFilterViewModel : ViewModelBase
    {
        private readonly IFilterLookupService lookups;
        private readonly DataFilter filter;
        private readonly IUserDialogs dialogs;
        private bool isSettingProgrammatically;
        private int boreholesVersion;

        public RibbonFilterViewModel(IFilterLookupService lookups, DataFilter filter, IUserDialogs dialogs)
        {
            this.lookups = lookups;
            this.filter = filter;
            this.dialogs = dialogs;
            Pending = Task.CompletedTask;
        }

        /// <summary>Последняя запущенная из свойств загрузка (чтобы тесты могли дождаться её окончания).</summary>
        public Task Pending { get; private set; }

        public IReadOnlyList<InterbedItem> Interbeds
        {
            get => GetValue<IReadOnlyList<InterbedItem>>() ?? Array.Empty<InterbedItem>();
            private set => SetValue(value);
        }

        public IReadOnlyList<SeamItem> Seams
        {
            get => GetValue<IReadOnlyList<SeamItem>>() ?? Array.Empty<SeamItem>();
            private set => SetValue(value);
        }

        public IReadOnlyList<LineItem> Lines
        {
            get => GetValue<IReadOnlyList<LineItem>>() ?? Array.Empty<LineItem>();
            private set => SetValue(value);
        }

        public IReadOnlyList<LineBoreholeItem> Boreholes
        {
            get => GetValue<IReadOnlyList<LineBoreholeItem>>() ?? Array.Empty<LineBoreholeItem>();
            private set => SetValue(value);
        }

        public int? SelectedInterbedId
        {
            get => GetValue<int?>();
            set
            {
                if (Equals(GetValue<int?>(), value))
                    return;

                SetValue(value);
                filter.InterbedId = value;
            }
        }

        public int? SelectedSeamId
        {
            get => GetValue<int?>();
            set
            {
                if (Equals(GetValue<int?>(), value))
                    return;

                SetValue(value);
                filter.SeamId = value;
            }
        }

        public int? SelectedLineId
        {
            get => GetValue<int?>();
            set
            {
                if (Equals(GetValue<int?>(), value))
                    return;

                SetValue(value);
                filter.LineId = value;
                ReloadBoreholesAfterUserChange();
            }
        }

        public int? SelectedBoreholeId
        {
            get => GetValue<int?>();
            set
            {
                if (Equals(GetValue<int?>(), value))
                    return;

                SetValue(value);
                filter.BoreholeId = value;
            }
        }

        public bool AllBoreholes
        {
            get => GetValue<bool>();
            set
            {
                if (GetValue<bool>() == value)
                    return;

                SetValue(value);
                filter.AllBoreholes = value;
                ReloadBoreholesAfterUserChange();
            }
        }

        /// <summary>
        /// Обновляет списки тех групп, которые нужны экрану (вызывается при переходе на экран).
        /// Текущий выбор сохраняется, если такой элемент остался в списке, иначе выбирается первый.
        /// </summary>
        public async Task RefreshAsync(RibbonGroups groups)
        {
            try
            {
                if (groups.HasFlag(RibbonGroups.Seam))
                {
                    var seams = await lookups.GetSeamsAsync();
                    Seams = seams;
                    SelectedSeamId = KeepOrFirst(SelectedSeamId, seams.Select(s => s.SeamId));
                }

                if (groups.HasFlag(RibbonGroups.Interbed))
                {
                    var interbeds = await lookups.GetInterbedsAsync();
                    Interbeds = interbeds;
                    SelectedInterbedId = KeepOrFirst(SelectedInterbedId, interbeds.Select(i => i.InterbedId));
                }

                if (groups.HasFlag(RibbonGroups.LineBorehole))
                {
                    var lines = await lookups.GetLinesAsync();
                    Lines = lines;
                    SetSelectedLineWithoutReload(KeepOrFirst(SelectedLineId, lines.Select(l => l.LineId)));
                    await LoadBoreholesAsync(keepSelection: true);
                }
            }
            catch (Exception ex)
            {
                ShowLoadError(ex);
            }
        }

        private void ReloadBoreholesAfterUserChange()
        {
            if (isSettingProgrammatically)
                return;

            Pending = RunSafelyAsync(() => LoadBoreholesAsync(keepSelection: false));
        }

        /// <summary>Загружает скважины для выбранной линии (или все) и выбирает в списке первую либо прежнюю.</summary>
        private async Task LoadBoreholesAsync(bool keepSelection)
        {
            var version = ++boreholesVersion;
            var items = await lookups.GetLineBoreholesAsync(SelectedLineId, AllBoreholes);
            if (version != boreholesVersion)
                return;   // за время загрузки выбор линии снова сменили: этот результат устарел

            Boreholes = items;
            var current = keepSelection ? SelectedBoreholeId : null;
            SelectedBoreholeId = KeepOrFirst(current, items.Select(b => b.BoreholeId));
        }

        private void SetSelectedLineWithoutReload(int? lineId)
        {
            isSettingProgrammatically = true;
            try
            {
                SelectedLineId = lineId;
            }
            finally
            {
                isSettingProgrammatically = false;
            }
        }

        private static int? KeepOrFirst(int? current, IEnumerable<int> keys)
        {
            var list = keys.ToList();
            if (current != null && list.Contains(current.Value))
                return current;
            return list.Count > 0 ? list[0] : (int?)null;
        }

        private async Task RunSafelyAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                ShowLoadError(ex);
            }
        }

        private void ShowLoadError(Exception ex) =>
            dialogs.Show(
                ex.InnerException?.Message ?? ex.Message,
                "Ошибка загрузки",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
    }
}
