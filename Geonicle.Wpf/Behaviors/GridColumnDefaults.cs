using DevExpress.Xpf.Grid;
using System.Windows;

namespace Geonicle.Wpf.Behaviors
{
    /// <summary>
    /// Общие настройки колонок грида в одном месте (сейчас: AllowIncrementalSearch для всех колонок).
    /// Колонки генерируются автоматически, поэтому настройка применяется в AutoGeneratingColumn,
    /// а уже существующие колонки обрабатываются при включении свойства.
    /// Использование: behaviors:GridColumnDefaults.Apply="True" на GridControl.
    /// </summary>
    public static class GridColumnDefaults
    {
        public static readonly DependencyProperty ApplyProperty = DependencyProperty.RegisterAttached(
            "Apply", typeof(bool), typeof(GridColumnDefaults), new PropertyMetadata(false, OnApplyChanged));

        public static bool GetApply(DependencyObject obj) => (bool)obj.GetValue(ApplyProperty);

        public static void SetApply(DependencyObject obj, bool value) => obj.SetValue(ApplyProperty, value);

        private static void OnApplyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GridControl grid)
                return;

            grid.AutoGeneratingColumn -= Grid_AutoGeneratingColumn;

            if (e.NewValue is true)
            {
                grid.AutoGeneratingColumn += Grid_AutoGeneratingColumn;
                foreach (var column in grid.Columns)
                    Configure(column);
            }
        }

        private static void Grid_AutoGeneratingColumn(object sender, AutoGeneratingColumnEventArgs e)
            => Configure(e.Column);

        private static void Configure(ColumnBase column)
        {
            column.AllowIncrementalSearch = true;
        }
    }
}
