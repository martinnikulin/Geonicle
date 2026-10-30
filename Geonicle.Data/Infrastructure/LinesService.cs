using Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace Geonicle.Infrastructure
{
    public class LinesService : EditableDataService, ILinesService
    {
        public LinesService(IDbContextFactory<GDB52Context> contextFactory)
            : base(contextFactory)
        {
        }

        public async Task<ObservableCollection<Line>> GetLinesAsync()
        {
            var context = await ResetContextAsync();
            await context.Lines
                .Include(l => l.LineHoles)
                .LoadAsync();
            return context.Lines.Local.ToObservableCollection();
        }

        public async Task<IReadOnlyList<BoreholeLookupItem>> GetBoreholeLookupAsync()
        {
            return await Context.Boreholes
                .AsNoTracking()
                .OrderBy(b => b.HoleName)
                .Select(b => new BoreholeLookupItem(b.BoreholeId, b.HoleName))
                .ToListAsync();
        }
    }

    public interface ILinesService : IEditableDataService
    {
        Task<ObservableCollection<Line>> GetLinesAsync();

        /// <summary>Список скважин для колонки «Скважина»; вызывать после GetLinesAsync.</summary>
        Task<IReadOnlyList<BoreholeLookupItem>> GetBoreholeLookupAsync();
    }
}
