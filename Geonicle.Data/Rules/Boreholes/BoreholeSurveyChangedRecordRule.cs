using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Geonicle.Rules.Boreholes
{
    /// <summary>
    /// Отметка в ChangedRecords о том, что у новой скважины нужно пересчитать замеры (TableName = 'Survey').
    /// Заменяет часть триггера AfterInsertHoles. Выполняется после записи, когда известен BoreholeId.
    /// </summary>
    public class BoreholeSurveyChangedRecordRule : ChangeRule<Borehole>
    {
        public override EntityState State => EntityState.Added;

        public override ChangePhase Phase => ChangePhase.AfterSave;

        public override string? ReplacesTrigger => "AfterInsertHoles";

        protected override void Apply(Borehole borehole, GDB52Context db)
        {
            db.ChangedRecords.Add(new ChangedRecord { TableName = "Survey", TableId = borehole.BoreholeId });
        }
    }
}
