using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Geonicle.Wpf.Views;

namespace Geonicle.Wpf.ViewModels
{
    /// <summary>Показывает окна «Анализы» (немодально): по одному на пробу, владелец — главное окно.</summary>
    public class AssaysWindowService : IAssaysWindowService
    {
        private readonly IServiceProvider services;
        private readonly Dictionary<int, AssaysWindow> windows = new Dictionary<int, AssaysWindow>();

        public AssaysWindowService(IServiceProvider services)
        {
            this.services = services;
        }

        public void Open(int sampleId, string title, Action saved)
        {
            if (windows.TryGetValue(sampleId, out var existing))
            {
                if (existing.WindowState == WindowState.Minimized)
                    existing.WindowState = WindowState.Normal;
                existing.Activate();
                return;
            }

            var viewModel = services.GetRequiredService<AssaysViewModel>();
            viewModel.Saved += (_, _) => saved();
            var window = new AssaysWindow(viewModel) { Owner = Application.Current?.MainWindow };
            window.Closed += (_, _) => windows.Remove(sampleId);
            windows.Add(sampleId, window);
            window.Show();

            // Загрузка идёт после показа окна: ошибки показываются пользователю самой моделью.
            _ = viewModel.InitializeAsync(sampleId, title);
        }

        public void CloseAll()
        {
            foreach (var window in windows.Values.ToList())
                window.Close();
        }
    }
}
