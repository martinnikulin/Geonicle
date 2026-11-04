using DevExpress.Utils;
using DevExpress.Xpf.Grid;
using Geonicle.Data.Services;
using Geonicle.Wpf.ViewModels;

namespace Geonicle.Wpf.Views.Grids
{
    public partial class SamplesGrid
    {
        public SamplesGrid()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Двойной щелчок по нередактируемой ячейке (N, ID, колонки Ad, «Принято» в режиме всей скважины) открывает окно «Анализы» пробы.
        /// В редактируемых колонках двойной щелчок по-прежнему начинает правку.
        /// </summary>
        private void View_RowDoubleClick(object sender, RowDoubleClickEventArgs e)
        {
            var column = e.HitInfo?.Column;
            if (column == null || DataContext is not BoreholeDataViewModel viewModel)
                return;

            var editable = !column.ReadOnly && column.AllowEditing != DefaultBoolean.False;
            if (editable)
                return;

            if (sender is TableView view && view.Grid.GetRow(e.HitInfo.RowHandle) is SampleRow row)
                viewModel.OpenAssays(row);
        }
    }
}
