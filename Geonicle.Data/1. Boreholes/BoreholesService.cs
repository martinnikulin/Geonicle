using Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Geonicle.Data.Services
{
    public class BoreholesService : EditableDataService, IBoreholesService
    {
        public BoreholesService(IDbContextFactory<GDB52Context> contextFactory)
            : base(contextFactory)
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
        // Раньше его создавал триггер БД AfterInsertHoles.
        private static void Boreholes_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems == null)
                return;

            foreach (var borehole in e.NewItems.OfType<Borehole>())
                EnsureZeroSurvey(borehole);
        }

        /// <summary>Добавляет нулевой замер, если замера с глубиной 0 ещё нет (уникальный индекс BoreholeId + Depth).</summary>
        private static void EnsureZeroSurvey(Borehole borehole)
        {
            if (borehole.Surveys.Any(s => s.Depth == 0))
                return;

            borehole.Surveys.Add(new Survey { Depth = 0, Dip = 0, Azimuth = 0 });
        }
    }

    public interface IBoreholesService : IEditableDataService
    {
        Task<ObservableCollection<Borehole>> GetBoreholesAsync();
    }
}
