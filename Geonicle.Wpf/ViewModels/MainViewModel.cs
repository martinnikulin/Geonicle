using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using Geonicle.Wpf.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Geonicle.Wpf.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IServiceProvider services;

        public MainViewModel(IServiceProvider services)
        {
            this.services = services;
        }

        public object? CurrentViewModel
        {
            get => GetValue<object?>();
            private set => SetValue(value);
        }

        public void Navigate(Type viewModelType)
        {
            var previous = CurrentViewModel;
            CurrentViewModel = services.GetRequiredService(viewModelType);
            (previous as IDisposable)?.Dispose();
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (CurrentViewModel is ISelectedViewModel viewModel)
            {
                try
                {
                    await viewModel.SaveAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Ошибка сохранения",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

    }
}
