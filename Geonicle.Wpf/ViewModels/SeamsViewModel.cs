using Data.Models;
using Geonicle.Infrastructure;
using Geonicle.Wpf.Menus;
using Geonicle.Wpf.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Geonicle.Wpf.ViewModels
{
    public class SeamsViewModel : DataViewModelBase
    {
        private readonly ISeamsService dataService;

        public SeamsViewModel(ISeamsService dataService, IUserDialogs dialogs)
            : base(dialogs)
        {
            this.dataService = dataService;

            // Контекстные меню гридов экрана: пункты и команды объявляются здесь.
            SeamsMenu = CreateGridMenu();
            PartsMenu = CreateGridMenu();
            CoeffsMenu = CreateGridMenu();
        }

        public override string Title => "Пласты";

        public GridMenu SeamsMenu { get; }

        public GridMenu PartsMenu { get; }

        public GridMenu CoeffsMenu { get; }

        public ObservableCollection<Seam>? Seams
        {
            get => GetValue<ObservableCollection<Seam>?>();
            private set => SetValue(value);
        }

        public Seam? SelectedSeam
        {
            get => GetValue<Seam?>();
            set => SetValue(value);
        }

        /// <summary>Справочники для выпадающих списков грида коэффициентов.</summary>
        public IReadOnlyList<LookupItem>? InterbedLookup
        {
            get => GetValue<IReadOnlyList<LookupItem>?>();
            private set => SetValue(value);
        }

        public IReadOnlyList<LookupItem>? CoalTypeLookup
        {
            get => GetValue<IReadOnlyList<LookupItem>?>();
            private set => SetValue(value);
        }

        public IReadOnlyList<LookupItem>? CoalGradeLookup
        {
            get => GetValue<IReadOnlyList<LookupItem>?>();
            private set => SetValue(value);
        }

        protected override async Task LoadCoreAsync()
        {
            Seams = await dataService.GetSeamsAsync();
            InterbedLookup = await dataService.GetInterbedLookupAsync();
            CoalTypeLookup = await dataService.GetCoalTypeLookupAsync();
            CoalGradeLookup = await dataService.GetCoalGradeLookupAsync();
        }

        protected override Task SaveCoreAsync() => dataService.SaveAsync();

        protected override bool HasPendingChanges() => dataService.HasChanges;

        public override void Dispose() => dataService.Dispose();
    }
}
