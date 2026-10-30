using Data.Models;
using Geonicle.Infrastructure;
using Geonicle.Wpf.Menus;
using Geonicle.Wpf.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Geonicle.Wpf.ViewModels
{
    public class LinesViewModel : DataViewModelBase
    {
        private readonly ILinesService dataService;

        public LinesViewModel(ILinesService dataService, IUserDialogs dialogs)
            : base(dialogs)
        {
            this.dataService = dataService;

            // Контекстные меню гридов экрана: пункты и команды объявляются здесь.
            LinesMenu = CreateGridMenu();
            LineHolesMenu = CreateGridMenu();
        }

        public override string Title => "Разведочные линии";

        public GridMenu LinesMenu { get; }

        public GridMenu LineHolesMenu { get; }

        public ObservableCollection<Line>? Lines
        {
            get => GetValue<ObservableCollection<Line>?>();
            private set => SetValue(value);
        }

        public Line? SelectedLine
        {
            get => GetValue<Line?>();
            set => SetValue(value);
        }

        /// <summary>Список скважин для выпадающего списка в колонке «Скважина».</summary>
        public IReadOnlyList<BoreholeLookupItem>? BoreholeLookup
        {
            get => GetValue<IReadOnlyList<BoreholeLookupItem>?>();
            private set => SetValue(value);
        }

        protected override async Task LoadCoreAsync()
        {
            Lines = await dataService.GetLinesAsync();
            BoreholeLookup = await dataService.GetBoreholeLookupAsync();
        }

        protected override Task SaveCoreAsync() => dataService.SaveAsync();

        protected override bool HasPendingChanges() => dataService.HasChanges;

        public override void Dispose() => dataService.Dispose();
    }
}
