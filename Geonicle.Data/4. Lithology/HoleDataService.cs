using Data.Models;
using Geonicle.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace Geonicle.Data.Services
{
    /// <summary>Данные экрана «Данные по скважинам»: пластопересечения и литология выбранной скважины.</summary>
    public class HoleDataService : EditableDataService, IHoleDataService
    {
        private HoleData? currentData;

        public HoleDataService(IDbContextFactory<GDB52Context> contextFactory)
            : base(contextFactory)
        {
        }

        public async Task<HoleData> LoadAsync(int? boreholeId, int? interbedId)
        {
            var context = await ResetContextAsync();
            currentData = null;
            if (boreholeId == null || interbedId == null)
                return new HoleData(new ObservableCollection<SeamDef>(), new ObservableCollection<Lithology>());

            // Скважина с литологией загружается целиком: строки литологии грид добавляет в её коллекцию
            // (скважина проставляется сама), а валидатор проверяет уникальность глубин по этой же коллекции.
            var borehole = await context.Boreholes
                .Include(b => b.Lithologies)
                .SingleOrDefaultAsync(b => b.BoreholeId == boreholeId);
            await context.SeamDefs
                .Where(s => s.BoreholeId == boreholeId && s.InterbedId == interbedId)
                .Include(s => s.DepthFromNavigation)
                .Include(s => s.DepthToNavigation)
                .Include(s => s.Seam)
                .OrderBy(s => s.DepthFromNavigation.DepthFrom)
                .LoadAsync();

            var data = new HoleData(
                context.SeamDefs.Local.ToObservableCollection(),
                borehole == null ? new ObservableCollection<Lithology>() : (ObservableCollection<Lithology>)borehole.Lithologies);
            AssignSeamNames(data.HoleSeams, data.Lithology);
            currentData = data;
            return data;
        }

        /// <summary>Пересчитывает колонку «Пласт» у слоёв литологии (после сохранения глубины могли измениться).</summary>
        public void UpdateSeamNames()
        {
            if (currentData != null)
                AssignSeamNames(currentData.HoleSeams, currentData.Lithology);
        }

        /// <summary>Слой входит в пластопересечение, если лежит между его глубинами «От» и «До»; вне пластов название пустое.</summary>
        private static void AssignSeamNames(IEnumerable<SeamDef> seams, IEnumerable<Lithology> layers)
        {
            var list = seams.Where(s => s.DepthFrom != null && s.DepthTo != null).ToList();
            foreach (var layer in layers)
            {
                layer.SeamName = list
                    .FirstOrDefault(s => s.DepthFrom <= layer.DepthFrom && layer.DepthTo <= s.DepthTo)
                    ?.Seam?.Description;
            }
        }

        public async Task<IReadOnlyList<LookupItem>> GetSeamLookupAsync()
        {
            return await Context.Seams
                .AsNoTracking()
                .OrderBy(s => s.Description)
                .Select(s => new LookupItem(s.SeamId, s.Description))
                .ToListAsync();
        }

        public Task<IReadOnlyList<LookupItem>> GetCoalGradeLookupAsync() => GetDictionaryLookupAsync("марка");

        public Task<IReadOnlyList<LookupItem>> GetCoalTypeLookupAsync() => GetDictionaryLookupAsync("тип угля");

        public Task<IReadOnlyList<LookupItem>> GetFormationLookupAsync() => GetDictionaryLookupAsync("свита");

        public async Task<IReadOnlyList<LookupItem>> GetRockLookupAsync()
        {
            return await Context.Rocks
                .AsNoTracking()
                .OrderBy(r => r.RockId)
                .Select(r => new LookupItem(r.RockId, r.Description))
                .ToListAsync();
        }
    }

    /// <summary>Загруженные данные скважины. Пластопересечения — только выбранного прослоя, литология — вся скважина.</summary>
    public record HoleData(ObservableCollection<SeamDef> HoleSeams, ObservableCollection<Lithology> Lithology);

    public interface IHoleDataService : IEditableDataService
    {
        /// <summary>Загружает данные скважины; если скважина или прослой не выбраны, возвращает пустые списки.</summary>
        Task<HoleData> LoadAsync(int? boreholeId, int? interbedId);

        /// <summary>Пересчитать колонку «Пласт» у слоёв литологии (вызывается после сохранения).</summary>
        void UpdateSeamNames();

        // Справочники для колонок; вызывать после LoadAsync.
        Task<IReadOnlyList<LookupItem>> GetSeamLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetCoalGradeLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetCoalTypeLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetFormationLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetRockLookupAsync();
    }
}
