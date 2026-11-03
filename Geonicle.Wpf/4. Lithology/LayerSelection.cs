using Data.Models;
using System.Collections.Generic;

namespace Geonicle.Wpf.ViewModels
{
    /// <summary>Строки литологии, выбранные в гриде (в порядке показа), и признак «грид отсортирован по глубине».</summary>
    public record LayerSelection(IReadOnlyList<Lithology> Layers, bool SortedByDepth);
}
