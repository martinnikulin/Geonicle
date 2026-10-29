using DevExpress.Mvvm;
using Geonicle.Wpf.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Geonicle.Wpf.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IServiceProvider services;

        public MainViewModel(IServiceProvider services)
        {
            this.services = services;
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
                var answer = MessageBox.Show(
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

        /// <summary>Перечитывает данные текущего экрана. Заодно отменяет правки, которые не удаётся сохранить.</summary>
        private async Task RefreshAsync()
        {
            if (CurrentViewModel is not ISelectedViewModel viewModel)
                return;

            if (viewModel.HasChanges)
            {
                var answer = MessageBox.Show(
                    "Несохранённые изменения будут потеряны. Обновить данные?",
                    "Обновление",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (answer != MessageBoxResult.Yes)
                    return;
            }

            try
            {
                await viewModel.LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.InnerException?.Message ?? ex.Message,
                    "Ошибка загрузки",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static async Task<bool> TrySaveAsync(ISelectedViewModel viewModel)
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
                MessageBox.Show(
                    message,
                    "Ошибка сохранения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }
    }
}
