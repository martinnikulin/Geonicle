using Data.Models;
using Geonicle.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace Geonicle.Data.Services
{
    /// <summary>Данные экрана «Данные по скважинам»: пластопересечения и литология выбранной скважины.</summary>
    public class HoleDataService : EditableDataService, IHoleDataService
    {
        public HoleDataService(IDbContextFactory<GDB52Context> contextFactory)
            : base(contextFactory)
        {
        }

        public async Task<HoleData> LoadAsync(int? boreholeId, int? interbedId)
        {
            var context = await ResetContextAsync();
            if (boreholeId == null || interbedId == null)
                return new HoleData(new ObservableCollection<SeamDef>(), new ObservableCollection<Lithology>());

            // Скважина с литологией загружается целиком: строки литологии грид добавляет в её коллекцию
            // (скважина проставляется сама), а валидатор проверяет уникальность глубин по этой же коллекции.
            var borehole = await context.Boreholes
                .Include(b => b.Lithologies)
                .SingleOrDefaultAsync(b => b.BoreholeId == boreholeId);
            await context.SeamDefs
                .Where(s => s.BoreholeId == boreholeId && s.InterbedId == interbedId)
                .OrderBy(s => s.DepthFrom)
                .LoadAsync();

            return new HoleData(
                context.SeamDefs.Local.ToObservableCollection(),
                borehole == null ? new ObservableCollection<Lithology>() : (ObservableCollection<Lithology>)borehole.Lithologies);
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

        // Справочники для колонок; вызывать после LoadAsync.
        Task<IReadOnlyList<LookupItem>> GetSeamLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetCoalGradeLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetCoalTypeLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetFormationLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetRockLookupAsync();
    }
}
