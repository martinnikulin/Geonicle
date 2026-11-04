using Data.Models;
using Geonicle.Validation;
using Microsoft.EntityFrameworkCore;

namespace Geonicle.Data.Services
{
    /// <summary>
    /// Анализы одной пробы для формы «Анализы». Работает в собственном контексте на время вызова,
    /// поэтому не зависит от несохранённых правок экрана «Данные по скважинам».
    /// </summary>
    public class AssayService : IAssayService
    {
        private readonly IDbContextFactory<GDB52Context> contextFactory;

        public AssayService(IDbContextFactory<GDB52Context> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public async Task<IReadOnlyList<AssayRow>> LoadAsync(int sampleId)
        {
            using var context = await contextFactory.CreateDbContextAsync();

            // Показатели и группы — представления над справочной БД (только чтение); берём актуальные, показатель всегда в группе.
            var vars = await (
                from v in context.AssayVars.AsNoTracking()
                join g in context.AssayGroups.AsNoTracking() on v.GroupId equals g.GroupId
                where v.Actual && g.Actual
                orderby g.N, v.N
                select new AssayVarInfo(v.VarId, v.N, v.Description, g.GroupId, g.GroupName, g.N))
                .ToListAsync();

            var values = await context.Assays.AsNoTracking()
                .Where(a => a.SampleId == sampleId)
                .ToDictionaryAsync(a => a.VarId, a => a.Val);

            return BuildRows(vars, values);
        }

        /// <summary>Строки формы: по строке на актуальный показатель (порядок сохраняется), значение — из анализов пробы.</summary>
        public static IReadOnlyList<AssayRow> BuildRows(IEnumerable<AssayVarInfo> vars, IReadOnlyDictionary<int, string?> values) =>
            vars.Select(v => new AssayRow(
                    v.VarId, v.N, v.Description, v.GroupId, v.GroupName, v.GroupN,
                    values.TryGetValue(v.VarId, out var val) ? val : null))
                .ToList();

        public async Task SaveAsync(int sampleId, IReadOnlyList<AssayRow> rows)
        {
            using var context = await contextFactory.CreateDbContextAsync();

            // Работаем только со строками формы (актуальные показатели): значения неактуальных показателей у пробы не трогаем.
            var varIds = rows.Select(r => r.VarId).ToList();
            var existing = await context.Assays
                .Where(a => a.SampleId == sampleId && varIds.Contains(a.VarId))
                .ToDictionaryAsync(a => a.VarId);

            foreach (var row in rows)
            {
                var text = Normalize(row.Val);
                if (existing.TryGetValue(row.VarId, out var assay))
                {
                    if (text == null)
                        context.Assays.Remove(assay);          // значение очистили: строки анализа нет
                    else if (assay.Val != text)
                        assay.Val = text;                      // изменили; Value пересчитает триггер БД
                }
                else if (text != null)
                {
                    context.Assays.Add(new Assay { SampleId = sampleId, VarId = row.VarId, Val = text });
                }
            }

            var errors = EntityValidation.Validate(context);
            if (errors.Count > 0)
                throw new DataValidationException(errors);

            await context.SaveChangesAsync();   // одна транзакция: все изменения или ни одного
        }

        /// <summary>Значение без пробелов по краям; пустое (в том числе из одних пробелов) — null.</summary>
        private static string? Normalize(string? val)
        {
            var text = val?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }
    }

    /// <summary>Показатель вместе с группой (из представлений AssayVars и AssayGroups).</summary>
    public record AssayVarInfo(int VarId, double N, string Description, int GroupId, string GroupName, int GroupN);

    public interface IAssayService
    {
        /// <summary>Все актуальные показатели с группами (по порядку групп и показателей) и значениями пробы.</summary>
        Task<IReadOnlyList<AssayRow>> LoadAsync(int sampleId);

        /// <summary>
        /// Записывает значения: новые непустые добавляются, изменённые обновляются, очищенные удаляются, остальное не трогается.
        /// При ошибках проверки ничего не записывается (DataValidationException).
        /// </summary>
        Task SaveAsync(int sampleId, IReadOnlyList<AssayRow> rows);
    }
}
