using Data.Models;
using Geonicle.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Geonicle.Data.Services
{
    /// <summary>
    /// Данные экрана «Блоки подсчёта запасов». Списки пласта и прослоя (детали, варианты, скважины для списка) загружаются со
    /// сбросом контекста; блоки версии, варианта и детали — без сброса, поэтому правки ранее показанных блоков и вариантов
    /// не теряются при смене версии, варианта или детали. Все правки сохраняются вместе (SaveAsync).
    /// </summary>
    public class ReserveBlocksService : EditableDataService, IReserveBlocksService
    {
        private int? currentVersionId;
        private int? currentConditionId;
        private int? currentPartId;
        private IReadOnlyList<BlockHoleItem> holes = Array.Empty<BlockHoleItem>();

        public ReserveBlocksService(IDbContextFactory<GDB52Context> contextFactory)
            : base(contextFactory)
        {
        }

        public async Task<ReserveBlockLists> LoadListsAsync(int? seamId, int? interbedId)
        {
            var context = await ResetContextAsync();
            holes = Array.Empty<BlockHoleItem>();
            if (seamId == null)
                return new ReserveBlockLists(Array.Empty<LookupItem>(), new ObservableCollection<VariantRow>(), Array.Empty<LookupItem>(), holes);

            var parts = await context.Parts.AsNoTracking()
                .Where(p => p.SeamId == seamId)
                .OrderBy(p => p.N).ThenBy(p => p.PartId)
                .Select(p => new LookupItem(p.PartId, p.Npart))
                .ToListAsync();

            var variants = new ObservableCollection<VariantRow>();
            var conditions = new List<LookupItem>();
            if (interbedId != null)
            {
                // Варианты пласта в выбранном прослое; строки SeamConds отслеживаются (правится ссылка на вариант).
                // AsNoTracking в соединении нельзя: он отключил бы отслеживание всего запроса, и правка не сохранилась бы.
                var pairs = await (
                    from sc in context.SeamConds
                    join c in context.Conditions on sc.ConditionId equals c.Id
                    where sc.SeamId == seamId && c.InterbedId == interbedId
                    orderby c.Variant, c.Id
                    select new { Seam = sc, c.Variant, c.Variant1 })
                    .ToListAsync();
                foreach (var pair in pairs)
                    variants.Add(new VariantRow(pair.Seam, pair.Variant, pair.Variant1 ?? ""));

                conditions = await context.Conditions.AsNoTracking()
                    .Where(c => c.InterbedId == interbedId)
                    .OrderBy(c => c.Variant).ThenBy(c => c.Id)
                    .Select(c => new LookupItem(c.Id, c.Variant1 ?? ""))
                    .ToListAsync();

                // Пластопересечения пласта и прослоя для списка «Скважина» в блоке: «скважина, комментарий».
                var seams = await context.SeamDefs.AsNoTracking()
                    .Where(s => s.SeamId == seamId && s.InterbedId == interbedId)
                    .Select(s => new
                    {
                        s.HoleSeamId,
                        s.Borehole.HoleName,
                        s.Comments,
                        From = s.DepthFromNavigation.DepthFrom,
                        To = s.DepthToNavigation.DepthTo,
                    })
                    .ToListAsync();
                holes = seams
                    .OrderBy(s => s.HoleName).ThenBy(s => s.From).ThenBy(s => s.HoleSeamId)
                    .Select(s => new BlockHoleItem(s.HoleSeamId, HoleText(s.HoleName, s.Comments), s.From, s.To))
                    .ToList();
            }

            return new ReserveBlockLists(parts, variants, conditions, holes);
        }

        /// <summary>Текст пластопересечения в списке «Скважина» и в BlockUnits.HoleName: «скважина, комментарий» (без комментария — только скважина).</summary>
        public static string HoleText(string? holeName, string? comments) =>
            string.IsNullOrWhiteSpace(comments) ? holeName ?? "" : $"{holeName}, {comments}";

        public async Task<ObservableCollection<Block>> LoadBlocksAsync(int? versionId, int? conditionId, int? partId)
        {
            currentVersionId = versionId;
            currentConditionId = conditionId;
            currentPartId = partId;

            var blocks = new List<Block>();
            if (versionId != null && conditionId != null && partId != null)
            {
                var context = Context;
                blocks = await context.Blocks
                    .Where(b => b.VersionId == versionId && b.ConditionId == conditionId && b.PartId == partId)
                    .Include(b => b.BlockDetails)
                    .Include(b => b.BlockUnits)
                    .OrderBy(b => b.BlockId)
                    .ToListAsync();

                // От и До скважин блока — из пластопересечений (SeamDefs по HoleSeamId), только для показа.
                var seamIds = blocks.SelectMany(b => b.BlockUnits).Select(u => u.HoleSeamId).OfType<int>().Distinct().ToList();
                var depths = (await context.SeamDefs.AsNoTracking()
                        .Where(s => seamIds.Contains(s.HoleSeamId))
                        .Select(s => new { s.HoleSeamId, From = s.DepthFromNavigation.DepthFrom, To = s.DepthToNavigation.DepthTo })
                        .ToListAsync())
                    .ToDictionary(s => s.HoleSeamId);
                foreach (var unit in blocks.SelectMany(b => b.BlockUnits))
                {
                    if (unit.HoleSeamId is { } id && depths.TryGetValue(id, out var d))
                        unit.SetSeamInfo(d.From, d.To);
                }
            }

            var result = new ObservableCollection<Block>(blocks);
            result.CollectionChanged += Blocks_CollectionChanged;
            return result;
        }

        /// <summary>Блок добавлен или удалён в гриде: деталь, вариант и версия берутся с ленты, строка попадает в контекст.</summary>
        private void Blocks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (Block block in e.NewItems ?? Array.Empty<Block>())
            {
                block.VersionId = currentVersionId ?? block.VersionId;
                block.ConditionId = currentConditionId ?? block.ConditionId;
                block.PartId = currentPartId ?? block.PartId;
                Context.Blocks.Add(block);
            }
            foreach (Block block in e.OldItems ?? Array.Empty<Block>())
                Context.Blocks.Remove(block);   // связанные части и скважины блока EF удаляет вместе с ним (каскад в трекере)
        }

        /// <summary>
        /// В строке скважины блока выбрали или ввели название: если оно совпало с пластопересечением из списка,
        /// берётся его HoleSeamId (и От/До), иначе строка без скважины — HoleSeamId = NULL.
        /// </summary>
        public void ApplyHole(BlockUnit unit)
        {
            var item = holes.FirstOrDefault(h => h.Text == unit.HoleName);
            unit.HoleSeamId = item?.HoleSeamId;
            unit.SetSeamInfo(item?.DepthFrom, item?.DepthTo);
        }

        public async Task<IReadOnlyList<LookupItem>> GetVersionsAsync()
        {
            return await Context.Versions.AsNoTracking()
                .OrderBy(v => v.Id)
                .Select(v => new LookupItem(v.Id, v.VersionName ?? ""))
                .ToListAsync();
        }

        public Task<IReadOnlyList<LookupItem>> GetCoalTypeLookupAsync() => GetDictionaryLookupAsync("тип угля");

        public Task<IReadOnlyList<LookupItem>> GetCoalGradeLookupAsync() => GetDictionaryLookupAsync("марка");

        public Task<IReadOnlyList<LookupItem>> GetBoundaryLookupAsync() => GetDictionaryLookupAsync("boundary detail");

        public Task<IReadOnlyList<LookupItem>> GetPillarLookupAsync() => GetDictionaryLookupAsync("pillar");
    }

    /// <summary>Пластопересечение для списка «Скважина» в блоке.</summary>
    public record BlockHoleItem(int HoleSeamId, string Text, decimal? DepthFrom, decimal? DepthTo);

    /// <summary>Списки выбранных пласта и прослоя: детали, варианты, кондиции для ссылки на вариант и пластопересечения для списка «Скважина».</summary>
    public record ReserveBlockLists(
        IReadOnlyList<LookupItem> Parts,
        ObservableCollection<VariantRow> Variants,
        IReadOnlyList<LookupItem> ConditionLookup,
        IReadOnlyList<BlockHoleItem> Holes);

    public interface IReserveBlocksService : IEditableDataService
    {
        /// <summary>
        /// Загружает списки пласта и прослоя (со сбросом контекста и несохранённых правок). Без пласта списки пусты;
        /// без прослоя нет вариантов и скважин.
        /// </summary>
        Task<ReserveBlockLists> LoadListsAsync(int? seamId, int? interbedId);

        /// <summary>
        /// Блоки версии, варианта (ConditionId) и детали вместе с частями и скважинами. Контекст не сбрасывается: правки
        /// ранее показанных блоков остаются. Добавление и удаление блоков в возвращённой коллекции переносится в контекст.
        /// </summary>
        Task<ObservableCollection<Block>> LoadBlocksAsync(int? versionId, int? conditionId, int? partId);

        /// <summary>После выбора или ввода названия скважины в блоке проставляет HoleSeamId и От/До (или NULL, если названия нет в списке).</summary>
        void ApplyHole(BlockUnit unit);

        // Справочники для колонок; вызывать после LoadListsAsync.
        Task<IReadOnlyList<LookupItem>> GetVersionsAsync();

        Task<IReadOnlyList<LookupItem>> GetCoalTypeLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetCoalGradeLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetBoundaryLookupAsync();

        Task<IReadOnlyList<LookupItem>> GetPillarLookupAsync();
    }
}
