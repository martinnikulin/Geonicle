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
        private Borehole? currentBorehole;
        private int? currentInterbedId;

        /// <summary>Код «пласта без названия», к которому привязываются новые пластопересечения (по умолчанию 0; менять только в тестах).</summary>
        public int NoNameSeamId { get; init; } = Geonicle.Validation.SeamValidator.NoNameSeamId;

        public HoleDataService(IDbContextFactory<GDB52Context> contextFactory)
            : base(contextFactory)
        {
        }

        public async Task<HoleData> LoadAsync(int? boreholeId, int? interbedId)
        {
            var context = await ResetContextAsync();
            currentData = null;
            currentBorehole = null;
            currentInterbedId = interbedId;
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

            currentBorehole = borehole;
            var data = new HoleData(
                context.SeamDefs.Local.ToObservableCollection(),
                borehole == null ? new ObservableCollection<Lithology>() : (ObservableCollection<Lithology>)borehole.Lithologies);
            AssignSeamNames(data.HoleSeams, data.Lithology);
            currentData = data;
            return data;
        }

        public async Task<SeamDef> AddHoleSeamAsync(IReadOnlyList<Lithology> layers)
        {
            var (first, last) = Boundaries(layers);
            if (currentBorehole == null || currentData == null || currentInterbedId == null)
                throw new HoleDataException("Не выбрана скважина или прослой.");

            var seam = new SeamDef
            {
                BoreholeId = currentBorehole.BoreholeId,
                Borehole = currentBorehole,
                InterbedId = currentInterbedId,
                SeamId = NoNameSeamId,
                Seam = await FindNoNameSeamAsync(),
                DrawFrom = first.DepthFrom,   // стопки по умолчанию совпадают с глубинами границ
                DrawTo = last.DepthTo,
            };
            SetBoundaries(seam, first, last);
            Context.SeamDefs.Add(seam);       // попадает в HoleSeams (Local) сразу, до сохранения
            UpdateSeamNames();
            return seam;
        }

        public Task RedefineHoleSeamAsync(SeamDef seam, IReadOnlyList<Lithology> layers)
        {
            var (first, last) = Boundaries(layers);
            if (currentData == null || !currentData.HoleSeams.Contains(seam))
                throw new HoleDataException("Выберите пластопересечение.");

            SetBoundaries(seam, first, last);
            UpdateSeamNames();
            return Task.CompletedTask;
        }

        /// <summary>Первая и последняя из выбранных строк литологии: они задают верхнюю и нижнюю границы пластопересечения.</summary>
        private static (Lithology First, Lithology Last) Boundaries(IReadOnlyList<Lithology> layers)
        {
            if (layers.Count == 0)
                throw new HoleDataException("Выберите строки литологии, которые войдут в пластопересечение.");
            if (layers.Any(l => l.LithologyId == 0))
                throw new HoleDataException("Среди выбранных строк литологии есть несохранённые. Сохраните данные и повторите.");
            return (layers[0], layers[^1]);
        }

        private static void SetBoundaries(SeamDef seam, Lithology first, Lithology last)
        {
            seam.DepthFromId = first.LithologyId;
            seam.DepthFromNavigation = first;
            seam.DepthToId = last.LithologyId;
            seam.DepthToNavigation = last;
        }

        private async Task<Seam> FindNoNameSeamAsync() =>
            await Context.Seams.FindAsync(NoNameSeamId)
            ?? throw new HoleDataException($"В справочнике пластов нет «пласта без названия» (код {NoNameSeamId}). Добавьте его в базу данных.");

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

        /// <summary>
        /// Добавляет пластопересечение «без названия» (SeamId = 0): верхняя граница — первая из выбранных строк литологии,
        /// нижняя — последняя. В БД оно появится после сохранения. Ошибки, понятные пользователю, — HoleDataException.
        /// </summary>
        Task<SeamDef> AddHoleSeamAsync(IReadOnlyList<Lithology> layers);

        /// <summary>Меняет границы («От» и «До») пластопересечения по первой и последней из выбранных строк литологии.</summary>
        Task RedefineHoleSeamAsync(SeamDef seam, IReadOnlyList<Lithology> layers);

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
