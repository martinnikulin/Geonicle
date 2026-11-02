using Data.Models;
using Geonicle.Validation;
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

        /// <summary>Текущий контекст; доступен после первого ResetContextAsync (для дополнительных запросов экрана).</summary>
        protected GDB52Context Context =>
            context ?? throw new InvalidOperationException("Данные ещё не загружены.");

        public bool HasChanges => context?.ChangeTracker.HasChanges() ?? false;

        /// <summary>
        /// Проверяет добавленные и изменённые сущности (валидаторы из папки Validation) и сохраняет.
        /// Если есть ошибки, в БД ничего не уходит: бросается DataValidationException со списком.
        /// </summary>
        public async Task SaveAsync()
        {
            if (context == null)
                return;

            var errors = EntityValidation.Validate(context);
            if (errors.Count > 0)
                throw new DataValidationException(errors);

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
