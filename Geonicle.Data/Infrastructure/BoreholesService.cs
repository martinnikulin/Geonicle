using Data.Models;
using Geonicle.Triggers.Boreholes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Geonicle.Infrastructure
{
    public class BoreholesService : EditableDataService, IBoreholesService
    {
        public BoreholesService(IServiceScopeFactory scopeFactory)
            : base(scopeFactory)
        {
        }

        public async Task<ObservableCollection<Borehole>> GetBoreholesAsync()
        {
            var context = await ResetContextAsync();
            await context.Boreholes
                .Include(b => b.Surveys)
                .LoadAsync();

            var boreholes = context.Boreholes.Local.ToObservableCollection();
            boreholes.CollectionChanged += Boreholes_CollectionChanged;
            return boreholes;
        }

        // Новая строка в гриде сразу получает нулевой замер: он виден в «Инклинометрии» до сохранения.
        private static void Boreholes_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems == null)
                return;

            foreach (var borehole in e.NewItems.OfType<Borehole>())
                BoreholeCreatedTrigger.EnsureZeroSurvey(borehole);
        }
    }

    public interface IBoreholesService : IEditableDataService
    {
        Task<ObservableCollection<Borehole>> GetBoreholesAsync();
    }
}
