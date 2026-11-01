using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Geonicle.Infrastructure
{
    /// <summary>
    /// Владеет контекстом на время жизни экрана: правки накапливаются в нём до SaveAsync.
    /// Каждая загрузка начинает с нового контекста (в новой области DI), поэтому данные всегда свежие,
    /// а триггеры из папки Triggers получают тот же контекст, что и сервис.
    /// Наследники только описывают, что загружать.
    /// </summary>
    public abstract class EditableDataService : IEditableDataService
    {
        private readonly IServiceScopeFactory scopeFactory;
        private IServiceScope? scope;
        private GDB52Context? context;

        protected EditableDataService(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }

        /// <summary>Текущий контекст; доступен после первого ResetContextAsync (для дополнительных запросов экрана).</summary>
        protected GDB52Context Context =>
            context ?? throw new InvalidOperationException("Данные ещё не загружены.");

        public bool HasChanges => context?.ChangeTracker.HasChanges() ?? false;

        /// <summary>Сохранение в одной транзакции: запись, триггеры «после сохранения» и их записи фиксируются вместе.</summary>
        public async Task SaveAsync()
        {
            if (context == null)
                return;

            await using var transaction = await context.Database.BeginTransactionAsync();
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public void Dispose()
        {
            context = null;
            scope?.Dispose();
            scope = null;
        }

        /// <summary>Освобождает прежний контекст (вместе с несохранёнными правками) и создаёт новый.</summary>
        protected Task<GDB52Context> ResetContextAsync()
        {
            Dispose();
            scope = scopeFactory.CreateScope();
            context = scope.ServiceProvider.GetRequiredService<GDB52Context>();
            return Task.FromResult(context);
        }
    }

    public interface IEditableDataService : IDisposable
    {
        bool HasChanges { get; }

        Task SaveAsync();
    }
}
