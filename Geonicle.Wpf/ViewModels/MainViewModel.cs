using DevExpress.Mvvm;
using Geonicle.Wpf.Interfaces;
using Geonicle.Wpf.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Geonicle.Wpf.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IServiceProvider services;

        private readonly IUserDialogs dialogs;

        public MainViewModel(IServiceProvider services, IUserDialogs dialogs)
        {
            this.services = services;
            this.dialogs = dialogs;
            SaveCommand = new AsyncCommand(SaveAsync);
            RefreshCommand = new AsyncCommand(RefreshAsync);
        }

        public AsyncCommand SaveCommand { get; }

        public AsyncCommand RefreshCommand { get; }

        public object? CurrentViewModel
        {
            get => GetValue<object?>();
            private set => SetValue(value);
        }

        /// <summary>
        /// Переключает экран. Если на текущем есть несохранённые изменения, спрашивает пользователя.
        /// Возвращает false, если переход отменён и остаться нужно на текущем экране.
        /// </summary>
        public async Task<bool> NavigateAsync(Type viewModelType)
        {
            if (CurrentViewModel is ISelectedViewModel { HasChanges: true } current)
            {
                var answer = dialogs.Show(
                    "Есть несохранённые изменения. Сохранить их перед переходом?",
                    "Несохранённые изменения",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (answer == MessageBoxResult.Cancel)
                    return false;
                if (answer == MessageBoxResult.Yes && !await TrySaveAsync(current))
                    return false;
            }

            var previous = CurrentViewModel;
            CurrentViewModel = services.GetRequiredService(viewModelType);
            (previous as IDisposable)?.Dispose();
            return true;
        }

        private async Task SaveAsync()
        {
            if (CurrentViewModel is ISelectedViewModel viewModel)
                await TrySaveAsync(viewModel);
        }

        private async Task RefreshAsync()
        {
            if (CurrentViewModel is ISelectedViewModel viewModel)
                await viewModel.RefreshAsync();
        }

        private async Task<bool> TrySaveAsync(ISelectedViewModel viewModel)
        {
            try
            {
                await viewModel.SaveAsync();
                return true;
            }
            catch (Exception ex)
            {
                var message = (ex.InnerException?.Message ?? ex.Message)
                    + "\n\nИсправьте данные и повторите сохранение либо нажмите «Обновить», чтобы отменить все изменения.";
                dialogs.Show(
                    message,
                    "Ошибка сохранения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }
    }
}
