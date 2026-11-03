using DevExpress.Mvvm;
using Geonicle.Wpf.Interfaces;
using Geonicle.Wpf.Ribbon;

namespace Geonicle.Wpf.ViewModels
{
    public class LithologyViewModel : ViewModelBase, IRibbonContext
    {
        public string Title => "Данные по скважинам";

        // Экран «Данные по скважинам»: общая группа, фильтр по прослою и фильтр по линии и скважине.
        public RibbonGroups VisibleRibbonGroups =>
            RibbonGroups.Data | RibbonGroups.Interbed | RibbonGroups.LineBorehole;
    }
}
