using Data.Models;
using Geonicle.Infrastructure;
using Geonicle.Wpf.Menus;
using Geonicle.Wpf.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Geonicle.Wpf.ViewModels
{
    public class BoreholesViewModel : DataViewModelBase
    {
        private readonly IBoreholesService dataService;

        public BoreholesViewModel(IBoreholesService dataService, IDialogService dialogs)
            : base(dialogs)
        {
            this.dataService = dataService;

            // Контекстные меню гридов экрана: пункты и команды объявляются здесь.
            BoreholesMenu = CreateGridMenu();
            SurveysMenu = CreateGridMenu();
        }

        public override string Title => "Скважины";

        public GridMenu BoreholesMenu { get; }

        public GridMenu SurveysMenu { get; }

        public ObservableCollection<Borehole>? Boreholes
        {
            get => GetValue<ObservableCollection<Borehole>?>();
            private set => SetValue(value);
        }

        public Borehole? SelectedBorehole
        {
            get => GetValue<Borehole?>();
            set => SetValue(value);
        }

        protected override async Task LoadCoreAsync()
        {
            Boreholes = await dataService.GetBoreholesAsync();
        }

        protected override Task SaveCoreAsync() => dataService.SaveAsync();

        protected override bool HasPendingChanges() => dataService.HasChanges;

        public override void Dispose() => dataService.Dispose();
    }
}
