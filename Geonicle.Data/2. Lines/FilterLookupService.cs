using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Geonicle.Infrastructure
{
    /// <summary>Списки для фильтров на ленте. Только чтение: контекст создаётся на каждый запрос, без отслеживания.</summary>
    public interface IFilterLookupService
    {
        /// <summary>Актуальные разделяющие прослои из CondDefs (раздел «Прослой»).</summary>
        Task<IReadOnlyList<InterbedItem>> GetInterbedsAsync();

        /// <summary>Актуальные пласты (Seams.Actual) по порядку N.</summary>
        Task<IReadOnlyList<SeamItem>> GetSeamsAsync();

        Task<IReadOnlyList<LineItem>> GetLinesAsync();

        /// <summary>
        /// Скважины выбранной линии (по LineHoles). При <paramref name="allBoreholes"/> — все скважины из LineHoles
        /// без фильтра по линии и без повторов. Без линии и без «все скважины» список пуст.
        /// </summary>
        Task<IReadOnlyList<LineBoreholeItem>> GetLineBoreholesAsync(int? lineId, bool allBoreholes);
    }

    public class FilterLookupService : IFilterLookupService
    {
        private readonly IDbContextFactory<GDB52Context> contextFactory;

        public FilterLookupService(IDbContextFactory<GDB52Context> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public async Task<IReadOnlyList<InterbedItem>> GetInterbedsAsync()
        {
            await using var db = await contextFactory.CreateDbContextAsync();
            return await db.CondDefs
                .AsNoTracking()
                .Where(c => c.Section == "Прослой" && c.Actual)
                .OrderBy(c => c.N)
                .Select(c => new InterbedItem(c.N * 100, "Прослой " + c.CondName))
                .ToListAsync();
        }

        public async Task<IReadOnlyList<SeamItem>> GetSeamsAsync()
        {
            await using var db = await contextFactory.CreateDbContextAsync();
            return await db.Seams
                .AsNoTracking()
                .Where(s => s.Actual)
                .OrderBy(s => s.N)
                .ThenBy(s => s.SeamId)
                .Select(s => new SeamItem(s.SeamId, s.Description ?? ""))
                .ToListAsync();
        }

        public async Task<IReadOnlyList<LineItem>> GetLinesAsync()
        {
            await using var db = await contextFactory.CreateDbContextAsync();
            return await db.Lines
                .AsNoTracking()
                .OrderBy(l => l.N)
                .ThenBy(l => l.LineId)
                .Select(l => new LineItem(l.LineId, l.N, l.Description))
                .ToListAsync();
        }

        public async Task<IReadOnlyList<LineBoreholeItem>> GetLineBoreholesAsync(int? lineId, bool allBoreholes)
        {
            await using var db = await contextFactory.CreateDbContextAsync();

            if (allBoreholes)
            {
                var all = await db.LineHoles
                    .AsNoTracking()
                    .Select(h => new { h.BoreholeId, h.Borehole.HoleName })
                    .Distinct()
                    .OrderBy(h => h.HoleName)
                    .ToListAsync();
                return all.Select(h => new LineBoreholeItem(h.BoreholeId, null, h.HoleName)).ToList();
            }

            if (lineId == null)
                return new List<LineBoreholeItem>();

            return await db.LineHoles
                .AsNoTracking()
                .Where(h => h.LineId == lineId)
                .OrderBy(h => h.N)
                .ThenBy(h => h.Borehole.HoleName)
                .Select(h => new LineBoreholeItem(h.BoreholeId, h.N, h.Borehole.HoleName))
                .ToListAsync();
        }
    }
}
