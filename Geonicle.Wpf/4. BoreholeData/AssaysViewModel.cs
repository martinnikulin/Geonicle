using DevExpress.Mvvm;
using Geonicle.Data.Services;
using Geonicle.Validation;
using Geonicle.Wpf.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace Geonicle.Wpf.ViewModels
{
    /// <summary>
    /// Окно «Анализы» пробы: актуальные показатели по группам (группировка — в самом гриде), редактируется только значение.
    /// «ОК» записывает изменения в БД сразу (отдельно от кнопки «Сохранить» экрана) и закрывает окно,
    /// «Отмена» закрывает без сохранения.
    /// </summary>
    public class AssaysViewModel : ViewModelBase
    {
        private readonly IAssayService service;
        private readonly IUserDialogs dialogs;
        private int sampleId;
        private bool isLoaded;

        public AssaysViewModel(IAssayService service, IUserDialogs dialogs)
        {
            this.service = service;
            this.dialogs = dialogs;
            OkCommand = new AsyncCommand(OkAsync, () => isLoaded);
            CancelCommand = new AsyncCommand(() =>
            {
                Cancel();
                return Task.CompletedTask;
            });
        }

        /// <summary>Анализы записаны в БД.</summary>
        public event EventHandler? Saved;

        /// <summary>Окно нужно закрыть («ОК» после записи или «Отмена»).</summary>
        public event EventHandler? CloseRequested;

        public AsyncCommand OkCommand { get; }

        public AsyncCommand CancelCommand { get; }

        public string? Title
        {
            get => GetValue<string?>();
            private set => SetValue(value);
        }

        public ObservableCollection<AssayRow>? Rows
        {
            get => GetValue<ObservableCollection<AssayRow>?>();
            private set => SetValue(value);
        }

        public async Task InitializeAsync(int sampleId, string title)
        {
            this.sampleId = sampleId;
            Title = title;
            try
            {
                Rows = new ObservableCollection<AssayRow>(await service.LoadAsync(sampleId));
                isLoaded = true;
                OkCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                dialogs.Show(ex.InnerException?.Message ?? ex.Message, "Ошибка загрузки", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>«Отмена»: закрыть окно без сохранения.</summary>
        public void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

        public async Task OkAsync()
        {
            if (!isLoaded || Rows == null)
                return;

            try
            {
                await service.SaveAsync(sampleId, Rows);
            }
            catch (DataValidationException ex)
            {
                dialogs.Show(ex.ToUserMessage(), "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            catch (Exception ex)
            {
                dialogs.Show(ex.InnerException?.Message ?? ex.Message, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Saved?.Invoke(this, EventArgs.Empty);
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
