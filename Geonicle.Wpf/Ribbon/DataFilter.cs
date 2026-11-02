using System;

namespace Geonicle.Wpf.Ribbon
{
    /// <summary>
    /// Общий фильтр данных, который задают элементы на ленте. Живёт всё время работы приложения,
    /// выбор сохраняется при переключении экранов. Экраны читают значения и подписываются на Changed.
    /// </summary>
    public class DataFilter
    {
        private int? interbedId;
        private int? lineId;
        private int? boreholeId;
        private bool allBoreholes;

        /// <summary>Разделяющий прослой (N × 100); null — не выбран.</summary>
        public int? InterbedId
        {
            get => interbedId;
            set => Set(ref interbedId, value);
        }

        /// <summary>Разведочная линия; null — не выбрана.</summary>
        public int? LineId
        {
            get => lineId;
            set => Set(ref lineId, value);
        }

        /// <summary>Выбранная скважина; null — не выбрана.</summary>
        public int? BoreholeId
        {
            get => boreholeId;
            set => Set(ref boreholeId, value);
        }

        /// <summary>Список скважин показывает все скважины из LineHoles, без фильтра по линии.</summary>
        public bool AllBoreholes
        {
            get => allBoreholes;
            set => Set(ref allBoreholes, value);
        }

        public event EventHandler? Changed;

        private void Set<T>(ref T field, T value)
        {
            if (Equals(field, value))
                return;

            field = value;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
