using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Geonicle.Rules.Boreholes
{
    /// <summary>
    /// Новая скважина получает нулевой замер инклинометрии (глубина 0, угол 0, азимут 0).
    /// Заменяет часть триггера AfterInsertHoles. Запись в LineHoles больше не создаётся.
    /// </summary>
    public class BoreholeCreatedRule : ChangeRule<Borehole>
    {
        public override EntityState State => EntityState.Added;

        protected override void Apply(Borehole borehole, GDB52Context db)
        {
            // Если замер с нулевой глубиной уже добавлен вручную, второй не нужен (уникальный индекс BoreholeId + Depth).
            if (borehole.Surveys.Any(s => s.Depth == 0))
                return;

            db.Surveys.Add(new Survey { Borehole = borehole, Depth = 0, Dip = 0, Azimuth = 0 });
        }
    }
}
