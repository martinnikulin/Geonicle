using Microsoft.EntityFrameworkCore;

namespace Geonicle.Validation
{
    /// <summary>
    /// Проверка всех добавленных и изменённых сущностей контекста (и правил удаления для удаляемых) перед сохранением.
    /// Валидаторы (наследники RowValidator) находятся в сборке автоматически, регистрировать их не нужно.
    /// </summary>
    public static class EntityValidation
    {
        private static readonly IReadOnlyDictionary<Type, IRowValidator> Validators = typeof(EntityValidation).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(IRowValidator).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) != null)
            .Select(t => (IRowValidator)Activator.CreateInstance(t)!)
            .ToDictionary(v => v.EntityType);

        /// <summary>Возвращает ошибки в виде «Описание строки: сообщение»; пустой список, если всё верно.</summary>
        public static IReadOnlyList<string> Validate(DbContext context)
        {
            context.ChangeTracker.DetectChanges();

            var errors = new List<string>();
            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                    continue;
                if (!Validators.TryGetValue(entry.Entity.GetType(), out var validator))
                    continue;

                var description = validator.Describe(entry.Entity);
                if (entry.State == EntityState.Deleted)
                {
                    errors.AddRange(validator.ValidateDelete(entry.Entity).Select(message => $"{description}: {message}"));
                    continue;
                }

                foreach (var failure in validator.Validate(entry.Entity).Errors)
                    errors.Add($"{description}: {failure.ErrorMessage}");
            }

            // Одинаковые сообщения (например, об одном и том же дубликате у двух строк) показываем один раз.
            return errors.Distinct().ToList();
        }
    }
}
