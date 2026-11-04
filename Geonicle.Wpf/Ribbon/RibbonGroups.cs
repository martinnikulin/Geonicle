using System;

namespace Geonicle.Wpf.Ribbon
{
    /// <summary>Группы ленты, видимость которых зависит от открытого экрана.</summary>
    [Flags]
    public enum RibbonGroups
    {
        None = 0,

        /// <summary>«Сохранить», «Обновить», «Добавить строку», «Удалить строку»: на всех экранах с гридами.</summary>
        Data = 1,

        /// <summary>Список «Разделяющий прослой».</summary>
        Interbed = 2,

        /// <summary>Списки «Разведочная линия», «Скважины» и флажок «Все скважины».</summary>
        LineBorehole = 4,

        /// <summary>Список «Пласт» (актуальные пласты).</summary>
        Seam = 8
    }
}
