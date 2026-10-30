using Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace Geonicle.Infrastructure
{
    public class SeamsService : EditableDataService, ISeamsService
    {
        public SeamsService(IDbContextFactory<GDB52Context> contextFactory)
            : base(contextFactory)
        {
        }

        public async Task<ObservableCollection<Seam>> GetSeamsAsync()
        {
            var context = await ResetContextAsync();
            await context.Seams
                .Include(s => s.Parts)
                .Include(s => s.Coeffs)
                .AsSplitQuery()
                .LoadAsync();
            return context.Seams.Local.ToObservableCollection();
        }

        public async Task<IReadOnlyList<LookupItem>> GetInterbedLookupAsync()
        {
            return await Context.CondDefs
                .AsNoTracking()
                .Where(c => c.Section == "Прослой")
                .OrderBy(c => c.N)
                .Select(c => new LookupItem(c.N * 100, "Прослой " + c.CondName))
                .ToListAsync();
        }

        public Task<IReadOnlyList<LookupItem>> GetCoalTypeLookupAsync() => GetDictionaryLookupAsync("тип угля");

        public Task<IReadOnlyList<LookupItem>> GetCoalGradeLookupAsync() => GetDictionaryLookupAsync("марка");

        private async Task<IReadOnlyList<LookupItem>> GetDictionaryLookupAsync(string section)
        {
            return await Context.Dictionaries
                .AsNoTracking()
                .Where(d => d.Section == section)
                .OrderBy(d => d.N)
                .Select(d => new LookupItem(d.DictionaryId, d.Description))
                .ToListAsync();
        }
    }

    public interface ISeamsService : IEditableDataService
    {
        Task<ObservableCollection<Seam>> GetSeamsAsync();

        // Справочники для колонок коэффициентов; вызывать после GetSeamsAsync.
        Task<IReadOnlyList<LookupItem>> GetInterbedLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetCoalTypeLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetCoalGradeLookupAsync();
    }
}
