using Data.Models;
using Geonicle.Infrastructure;
using Geonicle.Wpf.Ribbon;
using Geonicle.Wpf.Services;
using Geonicle.Wpf.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Geonicle.Wpf
{
    public static class AppServices
    {
        /// <summary>Все регистрации приложения в одном месте: так их использует и App, и тест сборки контейнера.</summary>
        public static IServiceCollection AddGeonicleApp(this IServiceCollection collection, string connectionString)
        {
            collection.AddSingleton<IDbContextFactory<GDB52Context>>(new GDB52ContextFactory(connectionString));
            collection.AddSingleton<IFilterLookupService, FilterLookupService>();
            collection.AddTransient<IBoreholesService, BoreholesService>();
            collection.AddTransient<ILinesService, LinesService>();
            collection.AddTransient<ISeamsService, SeamsService>();

            collection.AddSingleton<IUserDialogs, UserDialogs>();
            collection.AddSingleton<DataFilter>();
            collection.AddSingleton<RibbonFilterViewModel>();
            collection.AddSingleton<MainViewModel>();
            collection.AddTransient<BoreholesViewModel>();
            collection.AddTransient<LinesViewModel>();
            collection.AddTransient<SeamsViewModel>();
            collection.AddTransient<LithologyViewModel>();

            return collection;
        }
    }
}
