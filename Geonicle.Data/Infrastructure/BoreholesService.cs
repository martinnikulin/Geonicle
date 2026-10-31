using Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace Geonicle.Infrastructure
{
    public class BoreholesService : EditableDataService, IBoreholesService
    {
        public BoreholesService(IDbContextFactory<GDB52Context> contextFactory)
            : base(contextFactory)
        {
        }

        public bool HasNewBoreholes =>
            Context.ChangeTracker.Entries<Borehole>().Any(e => e.State == EntityState.Added);

        public async Task<ObservableCollection<Borehole>> GetBoreholesAsync()
        {
            var context = await ResetContextAsync();
            await context.Boreholes
                .Include(b => b.Surveys)
                .LoadAsync();
            return context.Boreholes.Local.ToObservableCollection();
        }
    }

    public interface IBoreholesService : IEditableDataService
    {
        /// <summary>Есть ли несохранённые новые скважины (после их сохранения появятся строки, созданные правилами или триггером).</summary>
        bool HasNewBoreholes { get; }

        Task<ObservableCollection<Borehole>> GetBoreholesAsync();
    }
}
