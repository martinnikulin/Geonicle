using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Geonicle.Data.Services
{
    /// <summary>
    /// Общая работа экранов с пробами (SampleRow): загрузка SampleDef со связями HSSamps и значениями Ad из представления Samples,
    /// перечитывание связей и значений после сохранения. Используют «Данные по скважинам» и «Пластопересечения».
    /// </summary>
    internal static class SampleRowLoader
    {
        /// <summary>Пробы из запроса (SampleDef отслеживаются и правятся) по порядку глубины; значения Ad — из представления.</summary>
        public static async Task<List<SampleRow>> LoadAsync(GDB52Context context, IQueryable<SampleDef> query)
        {
            var defs = await query
                .Include(s => s.Hssamps)
                .OrderBy(s => s.DepthFrom)
                .ToListAsync();
            var ids = defs.Select(d => d.SampleId).ToList();
            var values = (await context.Samples.AsNoTracking().Where(v => ids.Contains(v.SampleId)).ToListAsync())
                .ToDictionary(v => v.SampleId);

            var rows = new List<SampleRow>();
            foreach (var def in defs)
            {
                var row = new SampleRow(def);
                row.SetValues(values.GetValueOrDefault(def.SampleId));
                rows.Add(row);
            }
            return rows;
        }

        /// <summary>
        /// После сохранения БД могла изменить связи проб с пластопересечениями (триггеры) и значения Ad:
        /// перечитывает их (несохранённых правок связей уже нет).
        /// </summary>
        public static async Task RefreshLinksAndValuesAsync(GDB52Context context, IReadOnlyCollection<SampleRow> rows)
        {
            var ids = rows.Select(r => r.SampleId).Where(id => id != 0).ToList();

            var existing = (await context.Hssamps.AsNoTracking().Where(h => ids.Contains(h.SampleId))
                    .Select(h => new { h.HoleSeamId, h.SampleId }).ToListAsync())
                .Select(h => (h.HoleSeamId, h.SampleId)).ToHashSet();
            foreach (var entry in context.ChangeTracker.Entries<Hssamp>().ToList())
            {
                var link = entry.Entity;
                if (existing.Contains((link.HoleSeamId, link.SampleId)))
                {
                    await entry.ReloadAsync();
                }
                else
                {
                    entry.State = EntityState.Detached;
                    link.SampleDef?.Hssamps.Remove(link);
                }
            }
            await context.Hssamps.Where(h => ids.Contains(h.SampleId)).LoadAsync();   // связи, добавленные триггером

            await RefreshValuesAsync(context, rows);
        }

        /// <summary>Перечитывает только значения Ad из представления Samples; связи и несохранённые правки не затрагиваются.</summary>
        public static async Task RefreshValuesAsync(GDB52Context context, IReadOnlyCollection<SampleRow> rows)
        {
            var ids = rows.Select(r => r.SampleId).Where(id => id != 0).ToList();
            var values = (await context.Samples.AsNoTracking().Where(v => ids.Contains(v.SampleId)).ToListAsync())
                .ToDictionary(v => v.SampleId);
            foreach (var row in rows)
                row.SetValues(values.GetValueOrDefault(row.SampleId));
        }
    }
}
