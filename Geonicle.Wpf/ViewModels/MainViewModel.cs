using DevExpress.Mvvm;
using Geonicle.Validation;
using Geonicle.Wpf.Interfaces;
using Geonicle.Wpf.Ribbon;
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

        public MainViewModel(IServiceProvider services, IUserDialogs dialogs, RibbonFilterViewModel filters)
        {
            this.services = services;
            this.dialogs = dialogs;
            Filters = filters;
            SaveCommand = new AsyncCommand(SaveAsync);
            RefreshCommand = new AsyncCommand(RefreshAsync);
        }

        /// <summary>Фильтры на ленте (пласт, прослой, линия, скважина).</summary>
        public RibbonFilterViewModel Filters { get; }

        public AsyncCommand SaveCommand { get; }

        public AsyncCommand RefreshCommand { get; }

        public object? CurrentViewModel
        {
            get => GetValue<object?>();
            private set => SetValue(value);
        }

        private RibbonGroups CurrentRibbonGroups =>
            (CurrentViewModel as IRibbonContext)?.VisibleRibbonGroups ?? RibbonGroups.None;

        /// <summary>Общая группа «Данные» видна на всех экранах с гридами.</summary>
        public bool IsDataGroupVisible => CurrentRibbonGroups.HasFlag(RibbonGroups.Data);

        public bool IsSeamGroupVisible => CurrentRibbonGroups.HasFlag(RibbonGroups.Seam);

        public bool IsInterbedGroupVisible => CurrentRibbonGroups.HasFlag(RibbonGroups.Interbed);

        public bool IsLineBoreholeGroupVisible => CurrentRibbonGroups.HasFlag(RibbonGroups.LineBorehole);

        private void RaiseRibbonGroupsChanged()
        {
            RaisePropertyChanged(nameof(IsDataGroupVisible));
            RaisePropertyChanged(nameof(IsSeamGroupVisible));
            RaisePropertyChanged(nameof(IsInterbedGroupVisible));
            RaisePropertyChanged(nameof(IsLineBoreholeGroupVisible));
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

            RaiseRibbonGroupsChanged();
            await Filters.RefreshAsync(CurrentRibbonGroups & (RibbonGroups.Seam | RibbonGroups.Interbed | RibbonGroups.LineBorehole));
            return true;
        }

        /// <summary>Сохраняет текущий экран; ошибки показывает пользователю (кнопка «Сохранить»).</summary>
        public async Task SaveAsync()
        {
            if (CurrentViewModel is ISelectedViewModel viewModel)
                await TrySaveAsync(viewModel);
        }

        /// <summary>Перечитывает данные текущего экрана (кнопка «Обновить»).</summary>
        public async Task RefreshAsync()
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
            catch (DataValidationException ex)
            {
                dialogs.Show(
                    ex.ToUserMessage(),
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
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
