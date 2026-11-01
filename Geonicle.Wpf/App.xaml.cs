using Data.Models;
using DevExpress.Xpf.Core;
using Geonicle.Infrastructure;
using Geonicle.Wpf.Services;
using Geonicle.Wpf.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Windows;

namespace Geonicle.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider? services;

        static App()
        {
            CompatibilitySettings.UseLightweightThemes = true;
            ApplicationThemeHelper.Preload(PreloadCategories.Core);
            ApplicationThemeHelper.ApplicationThemeName = Theme.Win11Light.Name;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            services = ConfigureServices(configuration);

            var window = new MainWindow
            {
                DataContext = services.GetRequiredService<MainViewModel>()
            };
            window.Show();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // Необработанная ошибка не должна закрывать приложение вместе с несохранёнными данными.
            MessageBox.Show(
                e.Exception.InnerException?.Message ?? e.Exception.Message,
                "Непредвиденная ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            services?.Dispose();
            base.OnExit(e);
        }

        private static ServiceProvider ConfigureServices(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("GDB52")
                ?? throw new InvalidOperationException("Не задана строка подключения ConnectionStrings:GDB52.");

            var collection = new ServiceCollection();

            collection.AddSingleton<IDbContextFactory<GDB52Context>>(new GDB52ContextFactory(connectionString));
            collection.AddTransient<IBoreholesService, BoreholesService>();
            collection.AddTransient<ILinesService, LinesService>();
            collection.AddTransient<ISeamsService, SeamsService>();

            collection.AddSingleton<IUserDialogs, UserDialogs>();
            collection.AddSingleton<MainViewModel>();
            collection.AddTransient<BoreholesViewModel>();
            collection.AddTransient<LinesViewModel>();
            collection.AddTransient<SeamsViewModel>();
            collection.AddTransient<LithologyViewModel>();

            return collection.BuildServiceProvider();
        }
    }
}
