using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Grid;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Geonicle.Wpf.Menus
{
    /// <summary>
    /// Присоединяемое свойство для любого TableView: строит контекстное меню ячеек из GridMenu модели.
    /// Команды передаются объектами, а не привязками, поэтому работают и в popup-окне меню.
    /// Использование: menus:GridContextMenu.Menu="{Binding МенюГрида}"
    /// </summary>
    public static class GridContextMenu
    {
        public static readonly DependencyProperty MenuProperty = DependencyProperty.RegisterAttached(
            "Menu", typeof(GridMenu), typeof(GridContextMenu), new PropertyMetadata(null, OnMenuChanged));

        private static readonly ConditionalWeakTable<TableView, List<BarButtonItem>> added =
            new ConditionalWeakTable<TableView, List<BarButtonItem>>();

        public static GridMenu? GetMenu(DependencyObject obj) => (GridMenu?)obj.GetValue(MenuProperty);

        public static void SetMenu(DependencyObject obj, GridMenu? value) => obj.SetValue(MenuProperty, value);

        private static void OnMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TableView view)
                return;

            // Тип элементов коллекции у DevExpress не публичный контракт для кода, поэтому работаем через IList.
            IList customizations = view.RowCellMenuCustomizations;

            if (added.TryGetValue(view, out var previous))
            {
                foreach (var item in previous)
                    customizations.Remove(item);
                previous.Clear();
            }
            else
            {
                previous = new List<BarButtonItem>();
                added.Add(view, previous);
            }

            if (e.NewValue is not GridMenu menu)
                return;

            foreach (var menuItem in menu.Items)
            {
                var barItem = new BarButtonItem
                {
                    Content = menuItem.Caption,
                    Command = menuItem.Command
                };
                customizations.Add(barItem);
                previous.Add(barItem);
            }
        }
    }
}
