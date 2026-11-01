using Data.Models;
using EntityFrameworkCore.Triggered;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Geonicle.DependencyInjection
{
    public static class DataServiceCollectionExtensions
    {
        /// <summary>Подключение к БД SQL Server. Все триггеры из папки Triggers находятся и подключаются автоматически.</summary>
        public static IServiceCollection AddGeonicleData(this IServiceCollection services, string connectionString)
            => services.AddGeonicleData(options => options.UseSqlServer(connectionString));

        /// <summary>Вариант с любым провайдером БД (например, InMemory в тестах).</summary>
        public static IServiceCollection AddGeonicleData(this IServiceCollection services, Action<DbContextOptionsBuilder> configureProvider)
        {
            var triggerTypes = typeof(DataServiceCollectionExtensions).Assembly
                .GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false } && t.GetInterfaces().Any(IsTriggerInterface))
                .ToList();

            // Контекст создаётся контейнером на каждую «единицу работы» (экран); триггеры получают тот же контекст.
            services.AddDbContext<GDB52Context>(options =>
            {
                configureProvider(options);
                options.EnableDetailedErrors();   // в ошибке чтения значения называет сущность и свойство
                options.UseTriggers(triggers =>
                {
                    foreach (var triggerType in triggerTypes)
                        triggers.AddTrigger(triggerType, ServiceLifetime.Scoped);
                });
            });

            return services;
        }

        private static bool IsTriggerInterface(Type type) =>
            type.IsGenericType && type.GetGenericTypeDefinition() is var definition &&
            (definition == typeof(IBeforeSaveTrigger<>) ||
             definition == typeof(IAfterSaveTrigger<>) ||
             definition == typeof(IAfterSaveFailedTrigger<>));
    }
}
