using DevExpress.Mvvm;
using Geonicle.Wpf.Interfaces;
using System;
using System.Threading.Tasks;

namespace Geonicle.Wpf.ViewModels
{
    /// <summary>
    /// Основа экрана с редактируемыми данными: загрузка, сохранение и признак несохранённых изменений.
    /// Наследник реализует только доступ к данным.
    /// </summary>
    public abstract class DataViewModelBase : ViewModelBase, ISelectedViewModel, IDisposable
    {
        protected DataViewModelBase()
        {
            InitializeCommand = new AsyncCommand(LoadAsync, () => !IsInitialized);
        }

        public abstract string Title { get; }

        /// <summary>Первая загрузка при открытии экрана (запускается из события Loaded).</summary>
        public AsyncCommand InitializeCommand { get; }

        public bool IsInitialized
        {
            get => GetValue<bool>();
            private set
            {
                SetValue(value);
                InitializeCommand.RaiseCanExecuteChanged();
            }
        }

        public bool HasChanges => IsInitialized && HasPendingChanges();

        public async Task LoadAsync()
        {
            await LoadCoreAsync();
            IsInitialized = true;
        }

        public Task SaveAsync() => SaveCoreAsync();

        protected abstract Task LoadCoreAsync();

        protected abstract Task SaveCoreAsync();

        protected abstract bool HasPendingChanges();

        public virtual void Dispose()
        {
        }
    }
}
