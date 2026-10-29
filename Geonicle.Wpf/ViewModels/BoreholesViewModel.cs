using CommunityToolkit.Mvvm.ComponentModel;
using Data.Models;
using DevExpress.Mvvm;
using DevExpress.Mvvm.CodeGenerators;
using Geonicle.Infrastructure;
using Geonicle.Wpf.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Geonicle.Wpf.ViewModels
{
    [GenerateViewModel]
    public partial class BoreholesViewModel : ViewModelBase, ISelectedViewModel, IDisposable
    {
        public string Title => "Скважины";

        [GenerateProperty]
        ObservableCollection<Borehole>? boreholes;

        [GenerateProperty]
        bool isInitialized;

        readonly IBoreholesService dataService;

        public BoreholesViewModel(IBoreholesService dataService)
        {
            this.dataService = dataService;
        }

        public void Dispose() => dataService.Dispose();

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
