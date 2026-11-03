using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models
{
    public partial class SeamDef
    {
        /// <summary>Глубина «От»: кровля слоя литологии DepthFromId (как в представлении HoleSeams). Только чтение.</summary>
        [NotMapped]
        public decimal? DepthFrom => DepthFromNavigation?.DepthFrom;

        /// <summary>Глубина «До»: подошва слоя литологии DepthToId (как в представлении HoleSeams). Только чтение.</summary>
        [NotMapped]
        public decimal? DepthTo => DepthToNavigation?.DepthTo;
    }
}
