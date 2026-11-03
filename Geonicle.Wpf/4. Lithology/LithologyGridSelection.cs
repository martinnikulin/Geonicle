using DevExpress.Xpf.Grid;
using Data.Models;
using Geonicle.Wpf.ViewModels;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Geonicle.Wpf.Behaviors
{
    /// <summary>
    /// Присоединяемое свойство для грида литологии: даёт модели экрана доступ к выбранным строкам и к сортировке грида.
    /// Модель читает их в момент вызова (доступность и выполнение пунктов меню «Добавить пластопересечение» и
    /// «Переопределить пластопересечение»), поэтому подписываться на события выбора и сортировки не нужно.
    /// Использование: behaviors:LithologyGridSelection.Enabled="True" на GridControl (DataContext — LithologyViewModel).
    /// </summary>
    public static class LithologyGridSelection
    {
        public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
            "Enabled", typeof(bool), typeof(LithologyGridSelection), new PropertyMetadata(false, OnEnabledChanged));

        public static bool GetEnabled(DependencyObject obj) => (bool)obj.GetValue(EnabledProperty);

        public static void SetEnabled(DependencyObject obj, bool value) => obj.SetValue(EnabledProperty, value);

        private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GridControl grid)
                return;

            grid.Loaded -= Grid_Loaded;
            grid.DataContextChanged -= Grid_DataContextChanged;
            if (e.NewValue is true)
            {
                grid.Loaded += Grid_Loaded;
                grid.DataContextChanged += Grid_DataContextChanged;
                Attach(grid);
            }
        }

        private static void Grid_Loaded(object sender, RoutedEventArgs e) => Attach((GridControl)sender);

        private static void Grid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) => Attach((GridControl)sender);

        private static void Attach(GridControl grid)
        {
            if (grid.DataContext is LithologyViewModel viewModel)
                viewModel.LayerSelectionProvider = () => Read(grid);
        }

        private static LayerSelection Read(GridControl grid)
        {
            // Обработчики строк выбранных данных идут в порядке показа: у отсортированного грида это порядок глубин.
            var layers = grid.GetSelectedRowHandles()
                .Where(handle => handle >= 0)
                .OrderBy(handle => handle)
                .Select(handle => grid.GetRow(handle))
                .OfType<Lithology>()
                .ToList();

            var sort = grid.SortInfo.FirstOrDefault();
            var sortedByDepth = sort != null
                && (sort.FieldName == nameof(Lithology.DepthFrom) || sort.FieldName == nameof(Lithology.DepthTo))
                && sort.SortOrder == ListSortDirection.Ascending;

            return new LayerSelection(layers, sortedByDepth);
        }
    }
}
