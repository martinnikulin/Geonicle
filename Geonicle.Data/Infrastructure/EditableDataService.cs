using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Geonicle.Infrastructure
{
    /// <summary>
    /// Владеет контекстом на время жизни экрана: правки накапливаются в нём до SaveAsync.
    /// Каждая загрузка начинает с нового контекста, поэтому данные всегда свежие.
    /// Наследники только описывают, что загружать.
    /// </summary>
    public abstract class EditableDataService : IEditableDataService
    {
        private readonly IDbContextFactory<GDB52Context> contextFactory;
        private GDB52Context? context;

        protected EditableDataService(IDbContextFactory<GDB52Context> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public bool HasChanges => context?.ChangeTracker.HasChanges() ?? false;

        public async Task SaveAsync()
        {
            if (context != null)
                await context.SaveChangesAsync();
        }

        public void Dispose()
        {
            context?.Dispose();
            context = null;
        }

        /// <summary>Освобождает прежний контекст (вместе с несохранёнными правками) и создаёт новый.</summary>
        protected async Task<GDB52Context> ResetContextAsync()
        {
            context?.Dispose();
            context = await contextFactory.CreateDbContextAsync();
            return context;
        }
    }

    public interface IEditableDataService : IDisposable
    {
        bool HasChanges { get; }

        Task SaveAsync();
    }
}
