using Data.Models;
using DevExpress.Xpf.Grid;

namespace Geonicle.Wpf.Views.Grids
{
    public partial class AveragesGrid
    {
        public AveragesGrid()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Тип, марка и показатель — части ключа среднего: у сохранённой строки их менять нельзя (EF не меняет ключ),
        /// у новой строки они задаются.
        /// </summary>
        private void View_ShowingEditor(object sender, ShowingEditorEventArgs e)
        {
            var key = e.Column.FieldName is nameof(Average.CoalType) or nameof(Average.CoalGrade) or nameof(Average.VarId);
            if (key && sender is TableView view && view.Grid.GetRow(e.RowHandle) is Average { IsNew: false })
                e.Cancel = true;
        }
    }
}
