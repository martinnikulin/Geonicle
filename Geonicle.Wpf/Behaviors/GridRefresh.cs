using DevExpress.Xpf.Grid;
using Geonicle.Wpf.ViewModels;
using System;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Geonicle.Wpf.Behaviors
{
    /// <summary>
    /// Присоединяемое свойство для GridControl: после успешного сохранения экрана (и по запросу экрана, RefreshRequested) грид перечитывает значения строк.
    /// Нужно, чтобы ключи, выданные БД при вставке (ID у новых строк), появлялись в гриде сразу:
    /// сущности об изменениях не уведомляют, и без этого ячейка остаётся пустой до обновления данных.
    /// Использование: behaviors:GridRefresh.RefreshOnSave="True" на GridControl (DataContext — модель экрана).
    /// </summary>
    public static class GridRefresh
    {
        public static readonly DependencyProperty RefreshOnSaveProperty = DependencyProperty.RegisterAttached(
            "RefreshOnSave", typeof(bool), typeof(GridRefresh), new PropertyMetadata(false, OnRefreshOnSaveChanged));

        private static readonly ConditionalWeakTable<GridControl, Subscription> Subscriptions = new ConditionalWeakTable<GridControl, Subscription>();

        public static bool GetRefreshOnSave(DependencyObject obj) => (bool)obj.GetValue(RefreshOnSaveProperty);

        public static void SetRefreshOnSave(DependencyObject obj, bool value) => obj.SetValue(RefreshOnSaveProperty, value);

        private static void OnRefreshOnSaveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GridControl grid)
                return;

            grid.Loaded -= Grid_Loaded;
            grid.Unloaded -= Grid_Unloaded;
            Unsubscribe(grid);

            if (e.NewValue is true)
            {
                grid.Loaded += Grid_Loaded;
                grid.Unloaded += Grid_Unloaded;
                if (grid.IsLoaded)
                    Subscribe(grid);
            }
        }

        private static void Grid_Loaded(object sender, RoutedEventArgs e) => Subscribe((GridControl)sender);

        private static void Grid_Unloaded(object sender, RoutedEventArgs e) => Unsubscribe((GridControl)sender);

        private static void Subscribe(GridControl grid)
        {
            Unsubscribe(grid);
            if (grid.DataContext is not DataViewModelBase viewModel)
                return;

            EventHandler handler = (_, _) => grid.RefreshData();
            viewModel.Saved += handler;
            viewModel.RefreshRequested += handler;
            Subscriptions.Add(grid, new Subscription(viewModel, handler));
        }

        private static void Unsubscribe(GridControl grid)
        {
            if (!Subscriptions.TryGetValue(grid, out var subscription))
                return;

            subscription.ViewModel.Saved -= subscription.Handler;
            subscription.ViewModel.RefreshRequested -= subscription.Handler;
            Subscriptions.Remove(grid);
        }

        private sealed class Subscription
        {
            public Subscription(DataViewModelBase viewModel, EventHandler handler)
            {
                ViewModel = viewModel;
                Handler = handler;
            }

            public DataViewModelBase ViewModel { get; }

            public EventHandler Handler { get; }
        }
    }
}
