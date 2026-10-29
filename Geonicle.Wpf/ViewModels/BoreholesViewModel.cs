using CommunityToolkit.Mvvm.ComponentModel;
using Data.Models;
using DevExpress.Mvvm;
using DevExpress.Mvvm.CodeGenerators;
using Geonicle.Infrastructure;
using Geonicle.Wpf.Interfaces;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Geonicle.Wpf.ViewModels
{
    [GenerateViewModel]
    public partial class BoreholesViewModel : ViewModelBase, ISelectedViewModel
    {
        public string Title => "Скважины";
        GDB52Context context;

        [GenerateProperty]
        ObservableCollection<Borehole>? boreholes;

        [GenerateProperty]
        bool isInitialized;

        readonly IBoreholesService dataService;

        public BoreholesViewModel()
        {
            context = new GDB52Context("Server=localhost;Database=GDB520001;Integrated Security=True;TrustServerCertificate=True");
            dataService = new BoreholesService(context);
        }

        [GenerateCommand]
        async Task InitializeAsync()
        {
            await LoadAsync();
        }

        public async Task LoadAsync()
        {
            Boreholes = await dataService.GetBoreholesAsync();
            IsInitialized = true;
            //Boreholes = new ObservableCollection<Borehole>(data);
        }

        [GenerateProperty]
        private Borehole? selectedBorehole;

        [GenerateCommand]
        public async Task SaveAsync()
        {
            await dataService.SaveAsync();
        }

        bool CanInitialize() => !IsInitialized;

        void OnUsernameChanged(string oldUsername) => InitializeAsyncCommand.RaiseCanExecuteChanged();
    }
}
