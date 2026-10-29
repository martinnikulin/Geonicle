using Data.Models;
using Geonicle.Infrastructure;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Geonicle.Wpf.ViewModels
{
    public class BoreholesViewModel : DataViewModelBase
    {
        private readonly IBoreholesService dataService;

        public BoreholesViewModel(IBoreholesService dataService)
        {
            this.dataService = dataService;
        }

        public override string Title => "Скважины";

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
