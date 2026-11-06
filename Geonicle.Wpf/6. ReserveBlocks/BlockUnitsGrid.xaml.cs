using Data.Models;
using DevExpress.Xpf.Grid;
using Geonicle.Wpf.ViewModels;

namespace Geonicle.Wpf.Views.Grids
{
    public partial class BlockUnitsGrid
    {
        public BlockUnitsGrid()
        {
            InitializeComponent();
        }

        /// <summary>
        /// В колонке «Скважина» выбрали из списка или ввели название: модель проставляет HoleSeamId и От/До
        /// (для названия вне списка HoleSeamId = NULL).
        /// </summary>
        private void View_CellValueChanged(object sender, CellValueChangedEventArgs e)
        {
            if (e.Column.FieldName == nameof(BlockUnit.HoleName)
                && DataContext is ReserveBlocksViewModel viewModel
                && sender is TableView view)
            {
                viewModel.UnitHoleChanged(view.Grid.GetRow(e.RowHandle) as BlockUnit);
            }
        }
    }
}
