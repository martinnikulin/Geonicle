using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Geonicle.Rules.Boreholes
{
    /// <summary>
    /// Новая скважина получает нулевой замер инклинометрии (глубина 0, угол 0, азимут 0).
    /// Заменяет часть триггера AfterInsertHoles. Запись в LineHoles больше не создаётся.
    /// Замер добавляется сразу при создании строки в гриде (BoreholesService), чтобы пользователь мог вводить
    /// следующие замеры до сохранения; при сохранении это правило страхует остальные пути добавления скважины.
    /// </summary>
    public class BoreholeCreatedRule : ChangeRule<Borehole>
    {
        public override EntityState State => EntityState.Added;

        protected override void Apply(Borehole borehole, GDB52Context db) => EnsureZeroSurvey(borehole);

        /// <summary>Добавляет нулевой замер, если замера с глубиной 0 ещё нет (уникальный индекс BoreholeId + Depth).</summary>
        public static void EnsureZeroSurvey(Borehole borehole)
        {
            if (borehole.Surveys.Any(s => s.Depth == 0))
                return;

            borehole.Surveys.Add(new Survey { Depth = 0, Dip = 0, Azimuth = 0 });
        }
    }
}
