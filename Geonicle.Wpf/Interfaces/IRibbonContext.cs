using Geonicle.Wpf.Ribbon;

namespace Geonicle.Wpf.Interfaces
{
    /// <summary>Экран сообщает, какие группы ленты ему нужны. Объявляется в модели экрана, в одном месте.</summary>
    public interface IRibbonContext
    {
        RibbonGroups VisibleRibbonGroups { get; }
    }
}
