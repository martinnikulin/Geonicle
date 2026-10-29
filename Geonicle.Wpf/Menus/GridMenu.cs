using System.Collections.Generic;
using System.Windows.Input;

namespace Geonicle.Wpf.Menus
{
    /// <summary>Пункт контекстного меню грида: подпись и команда.</summary>
    public class GridMenuItem
    {
        public GridMenuItem(string caption, ICommand command)
        {
            Caption = caption;
            Command = command;
        }

        public string Caption { get; }

        public ICommand Command { get; }
    }

    /// <summary>
    /// Описание контекстного меню одного грида. Объявляется в модели экрана,
    /// в представление попадает через GridContextMenu.Menu.
    /// </summary>
    public class GridMenu
    {
        private readonly List<GridMenuItem> items = new List<GridMenuItem>();

        public IReadOnlyList<GridMenuItem> Items => items;

        public GridMenu Add(string caption, ICommand command)
        {
            items.Add(new GridMenuItem(caption, command));
            return this;
        }
    }
}
