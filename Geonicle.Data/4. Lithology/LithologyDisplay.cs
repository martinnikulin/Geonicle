using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models
{
    public partial class Lithology
    {
        /// <summary>
        /// Название пласта, в пластопересечение которого входит слой (колонка «Пласт» в гриде литологии).
        /// Только для показа: заполняет HoleDataService при загрузке и после сохранения, в БД не пишется.
        /// </summary>
        [NotMapped]
        public string? SeamName { get; set; }
    }
}
