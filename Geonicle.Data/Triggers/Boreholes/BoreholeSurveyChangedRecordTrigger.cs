using Data.Models;
using EntityFrameworkCore.Triggered;

namespace Geonicle.Triggers.Boreholes
{
    /// <summary>
    /// Отметка в ChangedRecords о том, что у новой скважины нужно пересчитать замеры (TableName = 'Survey').
    /// Заменяет часть триггера БД AfterInsertHoles. Выполняется после записи, когда известен BoreholeId.
    /// </summary>
    public class BoreholeSurveyChangedRecordTrigger : IAfterSaveTrigger<Borehole>
    {
        private readonly GDB52Context db;

        public BoreholeSurveyChangedRecordTrigger(GDB52Context db)
        {
            this.db = db;
        }

        public async Task AfterSave(ITriggerContext<Borehole> context, CancellationToken cancellationToken)
        {
            if (context.ChangeType != ChangeType.Added)
                return;

            db.ChangedRecords.Add(new ChangedRecord { TableName = "Survey", TableId = context.Entity.BoreholeId });

            // Изменения, сделанные в триггере «после сохранения», библиотека сама не записывает.
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
