using Data.Models;
using Geonicle.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Geonicle.Data.Services
{
    /// <summary>Данные экрана «Данные по скважинам»: пластопересечения и литология выбранной скважины.</summary>
    public class BoreholeDataService : EditableDataService, IBoreholeDataService
    {
        private BoreholeData? currentData;
        private Borehole? currentBorehole;
        private int? currentInterbedId;
        private bool allowSampleChanges = true;

        /// <summary>Код «пласта без названия», к которому привязываются новые пластопересечения (по умолчанию 0; менять только в тестах).</summary>
        public int NoNameSeamId { get; init; } = Geonicle.Validation.SeamValidator.NoNameSeamId;

        public BoreholeDataService(IDbContextFactory<GDB52Context> contextFactory)
            : base(contextFactory)
        {
        }

        public async Task<BoreholeData> LoadAsync(int? boreholeId, int? interbedId)
        {
            var context = await ResetContextAsync();
            currentData = null;
            currentBorehole = null;
            currentInterbedId = interbedId;
            if (boreholeId == null || interbedId == null)
                return new BoreholeData(new ObservableCollection<SeamDef>(), new ObservableCollection<Lithology>());

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
            var samples = await LoadSamplesAsync(context, boreholeId.Value);

            var data = new BoreholeData(
                context.SeamDefs.Local.ToObservableCollection(),
                borehole == null ? new ObservableCollection<Lithology>() : (ObservableCollection<Lithology>)borehole.Lithologies,
                samples);
            AssignSeamNames(data.HoleSeams, data.Lithology);
            currentData = data;
            return data;
        }

        public async Task<SeamDef> AddHoleSeamAsync(IReadOnlyList<Lithology> layers)
        {
            var (first, last) = Boundaries(layers);
            if (currentBorehole == null || currentData == null || currentInterbedId == null)
                throw new BoreholeDataException("Не выбрана скважина или прослой.");

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
                throw new BoreholeDataException("Выберите пластопересечение.");

            SetBoundaries(seam, first, last);
            UpdateSeamNames();
            return Task.CompletedTask;
        }

        /// <summary>Первая и последняя из выбранных строк литологии: они задают верхнюю и нижнюю границы пластопересечения.</summary>
        private static (Lithology First, Lithology Last) Boundaries(IReadOnlyList<Lithology> layers)
        {
            if (layers.Count == 0)
                throw new BoreholeDataException("Выберите строки литологии, которые войдут в пластопересечение.");
            if (layers.Any(l => l.LithologyId == 0))
                throw new BoreholeDataException("Среди выбранных строк литологии есть несохранённые. Сохраните данные и повторите.");
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
            ?? throw new BoreholeDataException($"В справочнике пластов нет «пласта без названия» (код {NoNameSeamId}). Добавьте его в базу данных.");

        /// <summary>Пересчитывает колонку «Пласт» у слоёв литологии (после сохранения глубины могли измениться).</summary>
        public void UpdateSeamNames()
        {
            if (currentData != null)
                AssignSeamNames(currentData.HoleSeams, currentData.Lithology);
        }

        /// <summary>
        /// Пробы скважины: SampleDef (правится) со связями HSSamps плюс значения Ad из представления Samples (только чтение).
        /// Добавление и удаление строк в списке переносится в контекст (как для скважин и их замеров).
        /// </summary>
        private async Task<ObservableCollection<SampleRow>> LoadSamplesAsync(GDB52Context context, int boreholeId)
        {
            var defs = await context.SampleDefs
                .Where(s => s.BoreholeId == boreholeId)
                .Include(s => s.Hssamps)
                .OrderBy(s => s.DepthFrom)
                .ToListAsync();
            var values = (await context.Samples.AsNoTracking().Where(v => v.BoreholeId == boreholeId).ToListAsync())
                .ToDictionary(v => v.SampleId);

            var rows = new ObservableCollection<SampleRow>();
            foreach (var def in defs)
            {
                var row = new SampleRow(def);
                row.SetValues(values.GetValueOrDefault(def.SampleId));
                rows.Add(row);
            }
            rows.CollectionChanged += Samples_CollectionChanged;
            return rows;
        }

        private void Samples_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // В режиме пластопересечения пробы добавлять и удалять нельзя (как в Delphi): такая строка в базу не попадёт.
            if (!allowSampleChanges || currentBorehole == null)
                return;

            foreach (SampleRow row in e.NewItems ?? Array.Empty<SampleRow>())
            {
                row.Def.BoreholeId = currentBorehole.BoreholeId;
                Context.SampleDefs.Add(row.Def);
            }
            foreach (SampleRow row in e.OldItems ?? Array.Empty<SampleRow>())
                Context.SampleDefs.Remove(row.Def);
        }

        public void UpdateSampleView(int? holeSeamId, bool bySeam)
        {
            allowSampleChanges = !bySeam;
            if (currentData == null)
                return;

            foreach (var row in currentData.Samples)
                row.SetSeam(holeSeamId);

            // Показанные пробы определяются глубинами: в режиме пластопересечения — пробы, пересекающиеся с ним хотя бы
            // частично (как отбор в триггерах БД для HSSamps). Нумерация идёт по глубине среди показанных проб.
            var seam = bySeam ? currentData.HoleSeams.FirstOrDefault(s => s.HoleSeamId == holeSeamId) : null;
            var shown = currentData.Samples
                .Where(r => !bySeam || (seam is { DepthFrom: { } from, DepthTo: { } to } && r.DepthFrom < to && r.DepthTo > from))
                .OrderBy(r => r.DepthFrom)
                .ToList();
            foreach (var row in currentData.Samples)
                row.SetN(0);
            for (var i = 0; i < shown.Count; i++)
                shown[i].SetN(i + 1);
        }

        /// <summary>
        /// После сохранения БД могла изменить связи проб с пластопересечениями (триггеры) и значения Ad:
        /// перечитывает их и присваивает ключи новым пробам.
        /// </summary>
        public async Task RefreshSamplesAsync()
        {
            if (currentData == null)
                return;

            var context = Context;
            var ids = currentData.Samples.Select(r => r.SampleId).Where(id => id != 0).ToList();

            var existing = (await context.Hssamps.AsNoTracking().Where(h => ids.Contains(h.SampleId))
                    .Select(h => new { h.HoleSeamId, h.SampleId }).ToListAsync())
                .Select(h => (h.HoleSeamId, h.SampleId)).ToHashSet();
            foreach (var entry in context.ChangeTracker.Entries<Hssamp>().ToList())
            {
                var link = entry.Entity;
                if (existing.Contains((link.HoleSeamId, link.SampleId)))
                {
                    await entry.ReloadAsync();
                }
                else
                {
                    entry.State = EntityState.Detached;
                    link.SampleDef?.Hssamps.Remove(link);
                }
            }
            await context.Hssamps.Where(h => ids.Contains(h.SampleId)).LoadAsync();   // связи, добавленные триггером

            await RefreshSampleValuesAsync();
        }

        /// <summary>
        /// Перечитывает только значения Ad из представления Samples (например, после записи анализов пробы).
        /// Связи проб и несохранённые правки экрана не затрагиваются.
        /// </summary>
        public async Task RefreshSampleValuesAsync()
        {
            if (currentData == null)
                return;

            var ids = currentData.Samples.Select(r => r.SampleId).Where(id => id != 0).ToList();
            var values = (await Context.Samples.AsNoTracking().Where(v => ids.Contains(v.SampleId)).ToListAsync())
                .ToDictionary(v => v.SampleId);
            foreach (var row in currentData.Samples)
                row.SetValues(values.GetValueOrDefault(row.SampleId));
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

        /// <summary>Какой анализ принимается: в AdIndex хранится номер записи (Dictionary.N), а не её код.</summary>
        public Task<IReadOnlyList<LookupItem>> GetAdIndexLookupAsync() => GetDictionaryLookupAsync("Ad", codeIsN: true);

        public async Task<IReadOnlyList<LookupItem>> GetRockLookupAsync()
        {
            return await Context.Rocks
                .AsNoTracking()
                .OrderBy(r => r.RockId)
                .Select(r => new LookupItem(r.RockId, r.Description))
                .ToListAsync();
        }
    }

    /// <summary>Загруженные данные скважины. Пластопересечения — только выбранного прослоя, литология и пробы — вся скважина.</summary>
    public record BoreholeData(
        ObservableCollection<SeamDef> HoleSeams,
        ObservableCollection<Lithology> Lithology,
        ObservableCollection<SampleRow> Samples)
    {
        public BoreholeData(ObservableCollection<SeamDef> holeSeams, ObservableCollection<Lithology> lithology)
            : this(holeSeams, lithology, new ObservableCollection<SampleRow>())
        {
        }
    }

    public interface IBoreholeDataService : IEditableDataService
    {
        /// <summary>Загружает данные скважины; если скважина или прослой не выбраны, возвращает пустые списки.</summary>
        Task<BoreholeData> LoadAsync(int? boreholeId, int? interbedId);

        /// <summary>
        /// Добавляет пластопересечение «без названия» (SeamId = 0): верхняя граница — первая из выбранных строк литологии,
        /// нижняя — последняя. В БД оно появится после сохранения. Ошибки, понятные пользователю, — BoreholeDataException.
        /// </summary>
        Task<SeamDef> AddHoleSeamAsync(IReadOnlyList<Lithology> layers);

        /// <summary>Меняет границы («От» и «До») пластопересечения по первой и последней из выбранных строк литологии.</summary>
        Task RedefineHoleSeamAsync(SeamDef seam, IReadOnlyList<Lithology> layers);

        /// <summary>Пересчитать колонку «Пласт» у слоёв литологии (вызывается после сохранения).</summary>
        void UpdateSeamNames();

        /// <summary>
        /// Привязывает пробы к выбранному пластопересечению (колонка «Принято»), нумерует показанные пробы по глубине
        /// и разрешает или запрещает добавлять и удалять пробы (в режиме пластопересечения нельзя).
        /// </summary>
        void UpdateSampleView(int? holeSeamId, bool bySeam);

        /// <summary>Перечитать связи проб и значения Ad после сохранения (триггеры БД меняют связи).</summary>
        Task RefreshSamplesAsync();

        /// <summary>Перечитать только значения Ad (связи и несохранённые правки не трогаются): после записи анализов пробы.</summary>
        Task RefreshSampleValuesAsync();

        // Справочники для колонок; вызывать после LoadAsync.
        Task<IReadOnlyList<LookupItem>> GetSeamLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetCoalGradeLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetCoalTypeLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetFormationLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetRockLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetAdIndexLookupAsync();
    }
}
