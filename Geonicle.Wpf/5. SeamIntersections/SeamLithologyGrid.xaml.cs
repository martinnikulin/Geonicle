using Data.Models;
using DevExpress.Xpf.Grid;

namespace Geonicle.Wpf.Views.Grids
{
    public partial class SeamLithologyGrid
    {
        public SeamLithologyGrid()
        {
            InitializeComponent();
        }

        /// <summary>
        /// «В подсчёт» берётся из LithRes выбранного прослоя; если строки LithRes нет, это дефект данных (её создаёт триггер БД):
        /// значение пусто и не правится.
        /// </summary>
        private void View_ShowingEditor(object sender, ShowingEditorEventArgs e)
        {
            if (e.Column.FieldName == nameof(Lithology.Reserves)
                && sender is TableView view && view.Grid.GetRow(e.RowHandle) is Lithology { Reserves: null })
                e.Cancel = true;
        }
    }
}
