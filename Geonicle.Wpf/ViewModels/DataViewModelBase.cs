using DevExpress.Mvvm;
using Geonicle.Wpf.Interfaces;
using Geonicle.Wpf.Menus;
using Geonicle.Wpf.Ribbon;
using Geonicle.Wpf.Services;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Geonicle.Wpf.ViewModels
{
    /// <summary>
    /// Основа экрана с редактируемыми данными: загрузка, обновление, сохранение и признак несохранённых изменений.
    /// Наследник реализует только доступ к данным и объявляет контекстные меню своих гридов.
    /// </summary>
    public abstract class DataViewModelBase : ViewModelBase, ISelectedViewModel, IRibbonContext, IDisposable
    {
        private readonly IUserDialogs dialogs;

        protected DataViewModelBase(IUserDialogs dialogs)
        {
            this.dialogs = dialogs;
            InitializeCommand = new AsyncCommand(TryLoadAsync, () => !IsInitialized);
            RefreshCommand = new AsyncCommand(RefreshAsync);
        }

        public abstract string Title { get; }

        /// <summary>
        /// Какие группы ленты нужны экрану. По умолчанию только общая группа «Данные» (у всех экранов с гридами);
        /// экраны с фильтрами переопределяют и добавляют RibbonGroups.Interbed и/или RibbonGroups.LineBorehole.
        /// </summary>
        public virtual RibbonGroups VisibleRibbonGroups => RibbonGroups.Data;

        /// <summary>Первая загрузка при открытии экрана (запускается из события Loaded).</summary>
        public AsyncCommand InitializeCommand { get; }

        /// <summary>Перечитать данные экрана (кнопка в ленте и пункт контекстного меню).</summary>
        public AsyncCommand RefreshCommand { get; }

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

        public async Task RefreshAsync()
        {
            if (HasChanges)
            {
                var answer = dialogs.Show(
                    "Несохранённые изменения будут потеряны. Обновить данные?",
                    "Обновление",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (answer != MessageBoxResult.Yes)
                    return;
            }

            await TryLoadAsync();
        }

        /// <summary>Загрузка с показом ошибки пользователю (иначе ошибка команды остаётся незамеченной и экран пустой).</summary>
        private async Task TryLoadAsync()
        {
            try
            {
                await LoadAsync();
            }
            catch (Exception ex)
            {
                dialogs.Show(
                    ex.InnerException?.Message ?? ex.Message,
                    "Ошибка загрузки",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>Данные успешно сохранены (гриды обновляют значения, например ID новых строк).</summary>
        public event EventHandler? Saved;

        public async Task SaveAsync()
        {
            await SaveCoreAsync();
            Saved?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Новое контекстное меню грида со стандартными пунктами (сейчас «Обновить»).
        /// Экран добавляет свои пункты через .Add(...) и отдаёт меню отдельным свойством для каждого грида.
        /// </summary>
        protected GridMenu CreateGridMenu() => new GridMenu().Add("Обновить", RefreshCommand);

        protected abstract Task LoadCoreAsync();

        protected abstract Task SaveCoreAsync();

        protected abstract bool HasPendingChanges();

        public virtual void Dispose()
        {
        }
    }
}
