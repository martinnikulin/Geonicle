using CommunityToolkit.Mvvm.Input;
using DevExpress.Dialogs.Core.View;
using DevExpress.Mvvm;
using Geonicle.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace Geonicle.Wpf.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly Dictionary<Type, object> _viewModels = new Dictionary<Type, object>();

        public object? CurrentViewModel
        {
            get => GetValue<object?>();
            private set => SetValue(value);
        }

        public void Navigate(Type viewModelType)
        {
            //if (!_viewModels.TryGetValue(viewModelType, out var viewModel))
            var viewModel = Activator.CreateInstance(viewModelType)!;
            _viewModels[viewModelType] = viewModel;

            CurrentViewModel = viewModel;
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
