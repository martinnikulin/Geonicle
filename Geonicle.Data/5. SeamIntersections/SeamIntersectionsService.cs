using Data.Models;
using Geonicle.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Geonicle.Data.Services
{
    /// <summary>
    /// Данные экрана «Пластопересечения»: пластопересечения выбранного пласта и прослоя (по всем скважинам), средние по пласту
    /// и, для выбранного пластопересечения, литология и пробы. Все правки копятся в одном контексте до SaveAsync.
    /// </summary>
    public class SeamIntersectionsService : EditableDataService, ISeamIntersectionsService
    {
        private int? currentSeamId;
        private int? currentInterbedId;
        private IReadOnlyList<SampleRow> currentSamples = Array.Empty<SampleRow>();
        private SeamDef? detailsSeam;

        public SeamIntersectionsService(IDbContextFactory<GDB52Context> contextFactory)
            : base(contextFactory)
        {
        }

        public async Task<SeamIntersectionsData> LoadAsync(int? seamId, int? interbedId)
        {
            var context = await ResetContextAsync();
            currentSeamId = seamId;
            currentInterbedId = interbedId;
            currentSamples = Array.Empty<SampleRow>();
            detailsSeam = null;
            if (seamId == null || interbedId == null)
                return new SeamIntersectionsData(new ObservableCollection<SeamDef>(), new ObservableCollection<Average>());

            // Пласт загружается ради проверки средних (валидатор сравнивает строки по его коллекции Averages).
            await context.Seams.Where(s => s.SeamId == seamId).LoadAsync();

            await context.SeamDefs
                .Where(s => s.SeamId == seamId && s.InterbedId == interbedId)
                .Include(s => s.Borehole)
                .Include(s => s.DepthFromNavigation)
                .Include(s => s.DepthToNavigation)
                .OrderBy(s => s.Borehole.HoleName)
                .ThenBy(s => s.DepthFromNavigation.DepthFrom)
                .LoadAsync();

            await context.Averages
                .Where(a => a.SeamId == seamId && a.InterbedId == interbedId)
                .OrderBy(a => a.CoalType).ThenBy(a => a.CoalGrade).ThenBy(a => a.VarId)
                .LoadAsync();
            var averages = context.Averages.Local.ToObservableCollection();
            averages.CollectionChanged += Averages_CollectionChanged;

            // Пластопересечения здесь не добавляются и не удаляются: список — копия, а не живое представление контекста,
            // поэтому случайно добавленная строка (например, кнопкой ленты) в базу не попадёт.
            return new SeamIntersectionsData(new ObservableCollection<SeamDef>(context.SeamDefs.Local), averages);
        }

        /// <summary>Строка добавлена в грид средних: пласт и прослой берутся с ленты (в самой строке их нет).</summary>
        private void Averages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (Average row in e.NewItems ?? Array.Empty<Average>())
            {
                row.SeamId = currentSeamId ?? row.SeamId;
                row.InterbedId = currentInterbedId ?? row.InterbedId;
            }
        }

        public async Task<SeamIntersectionDetails> LoadDetailsAsync(SeamDef? seam)
        {
            detailsSeam = seam;
            currentSamples = Array.Empty<SampleRow>();
            if (seam is not { DepthFrom: { } from, DepthTo: { } to } || currentInterbedId == null)
                return new SeamIntersectionDetails(new ObservableCollection<Lithology>(), new ObservableCollection<SampleRow>());

            var context = Context;
            var boreholeId = seam.BoreholeId;
            var interbedId = currentInterbedId.Value;

            // Слои литологии — те, что лежат целиком внутри пластопересечения.
            var layers = await context.Lithologies
                .Where(l => l.BoreholeId == boreholeId && l.DepthFrom >= from && l.DepthTo <= to)
                .OrderBy(l => l.DepthFrom)
                .ToListAsync();
            var layerIds = layers.Select(l => l.LithologyId).ToList();
            var links = (await context.LithRes
                    .Where(r => r.InterbedId == interbedId && layerIds.Contains(r.LithologyId))
                    .ToListAsync())
                .ToDictionary(r => r.LithologyId);
            foreach (var layer in layers)
                layer.SetReservesLink(links.GetValueOrDefault(layer.LithologyId));

            // Пробы — те, что пересекаются с пластопересечением хотя бы частично (как отбор триггеров БД для HSSamps).
            currentSamples = await SampleRowLoader.LoadAsync(
                context,
                context.SampleDefs.Where(s => s.BoreholeId == boreholeId && s.DepthFrom < to && s.DepthTo > from));
            ApplySampleSeam();

            return new SeamIntersectionDetails(new ObservableCollection<Lithology>(layers), new ObservableCollection<SampleRow>(currentSamples));
        }

        /// <summary>Привязывает пробы к показанному пластопересечению («Принято») и нумерует их по глубине.</summary>
        private void ApplySampleSeam()
        {
            var seamId = detailsSeam?.HoleSeamId;
            foreach (var row in currentSamples)
                row.SetSeam(seamId);

            var ordered = currentSamples.OrderBy(r => r.DepthFrom).ToList();
            for (var i = 0; i < ordered.Count; i++)
                ordered[i].SetN(i + 1);
        }

        public async Task RefreshAfterSaveAsync()
        {
            // Триггеры БД меняют связи проб (HSSamps) и значения Ad: перечитываем.
            await SampleRowLoader.RefreshLinksAndValuesAsync(Context, currentSamples.ToList());
            ApplySampleSeam();
        }

        public Task RefreshSampleValuesAsync() => SampleRowLoader.RefreshValuesAsync(Context, currentSamples.ToList());

        public Task<IReadOnlyList<LookupItem>> GetCoalGradeLookupAsync() => GetDictionaryLookupAsync("марка");

        public Task<IReadOnlyList<LookupItem>> GetCoalTypeLookupAsync() => GetDictionaryLookupAsync("тип угля");

        /// <summary>Какой анализ принимается: в AdIndex хранится номер записи (Dictionary.N), а не её код.</summary>
        public Task<IReadOnlyList<LookupItem>> GetAdIndexLookupAsync() => GetDictionaryLookupAsync("Ad", codeIsN: true);

        /// <summary>Показатели для колонки «Показатель» средних (AssayVars.Description); все, не только актуальные: в данных могут быть и старые.</summary>
        public async Task<IReadOnlyList<LookupItem>> GetAssayVarLookupAsync()
        {
            return await Context.AssayVars
                .AsNoTracking()
                .OrderBy(v => v.N)
                .Select(v => new LookupItem(v.VarId, v.Description))
                .ToListAsync();
        }

        public async Task<IReadOnlyList<LookupItem>> GetRockLookupAsync()
        {
            return await Context.Rocks
                .AsNoTracking()
                .OrderBy(r => r.RockId)
                .Select(r => new LookupItem(r.RockId, r.Description))
                .ToListAsync();
        }
    }

    /// <summary>Верхняя часть экрана: пластопересечения и средние выбранного пласта и прослоя.</summary>
    public record SeamIntersectionsData(ObservableCollection<SeamDef> HoleSeams, ObservableCollection<Average> Averages);

    /// <summary>Нижняя часть экрана: литология и пробы выбранного пластопересечения.</summary>
    public record SeamIntersectionDetails(ObservableCollection<Lithology> Lithology, ObservableCollection<SampleRow> Samples);

    public interface ISeamIntersectionsService : IEditableDataService
    {
        /// <summary>Загружает пластопересечения и средние пласта и прослоя; если пласт или прослой не выбраны, возвращает пустые списки.</summary>
        Task<SeamIntersectionsData> LoadAsync(int? seamId, int? interbedId);

        /// <summary>
        /// Литология (слой целиком внутри пластопересечения) и пробы (пересекаются с ним) выбранного пластопересечения.
        /// Несохранённые правки ранее показанных слоёв и проб остаются в контексте.
        /// </summary>
        Task<SeamIntersectionDetails> LoadDetailsAsync(SeamDef? seam);

        /// <summary>После сохранения перечитать связи проб и значения Ad (связи меняют триггеры БД).</summary>
        Task RefreshAfterSaveAsync();

        /// <summary>Перечитать только значения Ad (связи и несохранённые правки не трогаются): после записи анализов пробы.</summary>
        Task RefreshSampleValuesAsync();

        // Справочники для колонок; вызывать после LoadAsync.
        Task<IReadOnlyList<LookupItem>> GetCoalGradeLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetCoalTypeLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetAdIndexLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetAssayVarLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetRockLookupAsync();
    }
}
