using DevExpress.Mvvm;
using Geonicle.Wpf.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

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

        private IMessageBoxService Dialogs => GetService<IMessageBoxService>();

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
                var answer = Dialogs.ShowMessage(
                    "Есть несохранённые изменения. Сохранить их перед переходом?",
                    "Несохранённые изменения",
                    MessageButton.YesNoCancel,
                    MessageIcon.Question);

                if (answer == MessageResult.Cancel)
                    return false;
                if (answer == MessageResult.Yes && !await TrySaveAsync(current))
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
                var answer = Dialogs.ShowMessage(
                    "Несохранённые изменения будут потеряны. Обновить данные?",
                    "Обновление",
                    MessageButton.YesNo,
                    MessageIcon.Warning);

                if (answer != MessageResult.Yes)
                    return;
            }

            try
            {
                await viewModel.LoadAsync();
            }
            catch (Exception ex)
            {
                Dialogs.ShowMessage(
                    ex.InnerException?.Message ?? ex.Message,
                    "Ошибка загрузки",
                    MessageButton.OK,
                    MessageIcon.Error);
            }
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
                Dialogs.ShowMessage(
                    message,
                    "Ошибка сохранения",
                    MessageButton.OK,
                    MessageIcon.Error);
                return false;
            }
        }
    }
}
