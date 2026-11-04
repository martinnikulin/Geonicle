using System;

namespace Geonicle.Wpf.ViewModels
{
    /// <summary>
    /// Окна «Анализы» проб (немодальные). Экран открывает окно пробы и закрывает все свои окна при уходе;
    /// сам показ окон — в реализации, чтобы модель экрана тестировалась без WPF.
    /// </summary>
    public interface IAssaysWindowService
    {
        /// <summary>
        /// Открывает окно анализов пробы; если оно уже открыто — выводит его на передний план.
        /// <paramref name="saved"/> вызывается после того, как анализы записаны в БД («ОК»).
        /// </summary>
        void Open(int sampleId, string title, Action saved);

        /// <summary>Закрывает все открытые окна без сохранения.</summary>
        void CloseAll();
    }
}
