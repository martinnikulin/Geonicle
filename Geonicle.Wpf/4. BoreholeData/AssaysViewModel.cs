using DevExpress.Data.Filtering;
using DevExpress.Mvvm;
using Geonicle.Data.Services;
using Geonicle.Validation;
using Geonicle.Wpf.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Geonicle.Wpf.ViewModels
{
    /// <summary>
    /// Окно «Анализы» пробы: актуальные показатели, редактируется только значение. Группы показателей — табы слева
    /// (флажок «По группам»); таб включает фильтр одного грида по группе, без флажка показываются все показатели.
    /// «ОК» записывает изменения в БД сразу (отдельно от кнопки «Сохранить» экрана) и закрывает окно,
    /// «Отмена» закрывает без сохранения.
    /// </summary>
    public class AssaysViewModel : ViewModelBase
    {
        private readonly IAssayService service;
        private readonly IUserDialogs dialogs;
        private int sampleId;
        private bool isLoaded;
        private bool groupByGroups = true;

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

        /// <summary>Группы показателей по порядку групп (AssayGroups.N): по табу на группу.</summary>
        public IReadOnlyList<AssayGroupItem> Groups
        {
            get => GetValue<IReadOnlyList<AssayGroupItem>?>() ?? new List<AssayGroupItem>();
            private set => SetValue(value);
        }

        /// <summary>Выбранный таб (группа): грид показывает только её показатели.</summary>
        public AssayGroupItem? SelectedGroup
        {
            get => GetValue<AssayGroupItem?>();
            set
            {
                SetValue(value);
                UpdateFilter();
            }
        }

        /// <summary>Флажок «По группам»: включён — табы групп и фильтр по выбранной группе; выключен — все показатели.</summary>
        public bool GroupByGroups
        {
            get => groupByGroups;
            set
            {
                if (groupByGroups == value)
                    return;
                groupByGroups = value;
                RaisePropertyChanged(nameof(GroupByGroups));
                UpdateFilter();
            }
        }

        /// <summary>Условие грида: показатели выбранной группы; null — без условия (все показатели).</summary>
        public CriteriaOperator? FilterCriteria
        {
            get => GetValue<CriteriaOperator?>();
            private set => SetValue(value);
        }

        private void UpdateFilter() =>
            FilterCriteria = GroupByGroups && SelectedGroup != null ? new BinaryOperator("GroupId", SelectedGroup.GroupId) : null;

        public async Task InitializeAsync(int sampleId, string title)
        {
            this.sampleId = sampleId;
            Title = title;
            try
            {
                var rows = await service.LoadAsync(sampleId);
                Rows = new ObservableCollection<AssayRow>(rows);
                Groups = rows.GroupBy(r => r.GroupId)
                    .Select(g => g.First())
                    .OrderBy(r => r.GroupN)
                    .Select(r => new AssayGroupItem(r.GroupId, r.GroupName))
                    .ToList();
                SelectedGroup = Groups.FirstOrDefault();
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

    /// <summary>Группа показателей для таба (из строк формы).</summary>
    public record AssayGroupItem(int GroupId, string GroupName);
}
