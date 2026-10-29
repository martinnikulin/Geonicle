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
        Task<ObservableCollection<Borehole>> GetBoreholesAsync();
    }
}
