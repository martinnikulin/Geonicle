using Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace Geonicle.Infrastructure
{
    /// <summary>
    /// Владеет контекстом на время жизни экрана: правки накапливаются в нём до SaveAsync.
    /// Каждая загрузка создаёт новый контекст, поэтому данные всегда свежие.
    /// </summary>
    public class BoreholesService : IBoreholesService
    {
        private readonly IDbContextFactory<GDB52Context> contextFactory;
        private GDB52Context? context;

        public BoreholesService(IDbContextFactory<GDB52Context> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public async Task<ObservableCollection<Borehole>> GetBoreholesAsync()
        {
            context?.Dispose();
            context = await contextFactory.CreateDbContextAsync();
            await context.Boreholes
                .Include(b => b.Surveys)
                .LoadAsync();
            return context.Boreholes.Local.ToObservableCollection();
        }

        public async Task SaveAsync()
        {
            if (context != null)
                await context.SaveChangesAsync();
        }

        public bool HasChanges => context?.ChangeTracker.HasChanges() ?? false;

        public void Dispose()
        {
            context?.Dispose();
            context = null;
        }
    }

    public interface IBoreholesService : IDisposable
    {
        bool HasChanges { get; }

        Task<ObservableCollection<Borehole>> GetBoreholesAsync();

        Task SaveAsync();
    }
}
