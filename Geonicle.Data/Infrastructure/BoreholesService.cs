using Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace Geonicle.Infrastructure
{
    public class BoreholesService : IBoreholesService
    {
        private readonly GDB52Context context;

        public BoreholesService(GDB52Context context)
        {
            this.context = context;
        }

        public async Task<ObservableCollection<Borehole>> GetBoreholesAsync()
        {
            await context.Boreholes
                .Include(b => b.Surveys)
                .LoadAsync();
            return context.Boreholes.Local.ToObservableCollection();
            //return await context.Boreholes.ToListAsync();
        }

        public async Task SaveAsync()
        {
            await context.SaveChangesAsync();
        }
    }
    public interface IBoreholesService
    {
        Task<ObservableCollection<Borehole>> GetBoreholesAsync();

        Task SaveAsync();
    }
}
