using System.Collections.ObjectModel;
using System.Windows;
using Data.Models;
using Geonicle.Data.Services;
using Geonicle.Infrastructure;
using Geonicle.Validation;
using Geonicle.Wpf.Ribbon;
using Geonicle.Wpf.ViewModels;
using Xunit;

namespace Geonicle.Wpf.Tests;

/// <summary>Заглушка сервиса экрана «Блоки подсчёта запасов»: списки по пласту и прослою, блоки по версии, варианту и детали.</summary>
public sealed class FakeReserveBlocksData : IReserveBlocksService
{
    public Dictionary<(int?, int?), ReserveBlockLists> Lists { get; } = new();

    public Dictionary<(int?, int?, int?), List<Block>> Blocks { get; } = new();

    public List<(int? SeamId, int? InterbedId)> ListCalls { get; } = new();

    public List<(int? VersionId, int? ConditionId, int? PartId)> BlockCalls { get; } = new();

    public List<BlockUnit> AppliedHoles { get; } = new();

    public bool Changed { get; set; }

    public int Saves { get; private set; }

    public bool Disposed { get; private set; }

    public Exception SaveError { get; set; }

    /// <summary>Если задан, выполняется внутри загрузки списков (для проверки смены фильтра во время загрузки).</summary>
    public Action<int?, int?> OnLoadLists { get; set; }

    /// <summary>Если задан, перехватывает загрузку блоков (для проверки гонок).</summary>
    public Func<int?, int?, int?, Task<ObservableCollection<Block>>> BlocksHandler { get; set; }

    public bool HasChanges => Changed;

    public Task<ReserveBlockLists> LoadListsAsync(int? seamId, int? interbedId)
    {
        ListCalls.Add((seamId, interbedId));
        OnLoadLists?.Invoke(seamId, interbedId);
        Changed = false;
        return Task.FromResult(Lists.TryGetValue((seamId, interbedId), out var lists) ? lists : EmptyLists());
    }

    public static ReserveBlockLists EmptyLists() =>
        new(Array.Empty<LookupItem>(), new ObservableCollection<VariantRow>(), Array.Empty<LookupItem>(), Array.Empty<BlockHoleItem>());

    public Task<ObservableCollection<Block>> LoadBlocksAsync(int? versionId, int? conditionId, int? partId)
    {
        BlockCalls.Add((versionId, conditionId, partId));
        if (BlocksHandler != null)
            return BlocksHandler(versionId, conditionId, partId);
        return Task.FromResult(new ObservableCollection<Block>(
            Blocks.TryGetValue((versionId, conditionId, partId), out var blocks) ? blocks : new List<Block>()));
    }

    public void ApplyHole(BlockUnit unit) => AppliedHoles.Add(unit);

    public Task SaveAsync()
    {
        if (SaveError != null)
            throw SaveError;
        Saves++;
        Changed = false;
        return Task.CompletedTask;
    }

    private static Task<IReadOnlyList<LookupItem>> Items(params (int Id, string Name)[] items) =>
        Task.FromResult<IReadOnlyList<LookupItem>>(items.Select(i => new LookupItem(i.Id, i.Name)).ToList());

    public Task<IReadOnlyList<LookupItem>> GetVersionsAsync() => Items((2, "Альтернативная"), (1, "Основная"));

    public Task<IReadOnlyList<LookupItem>> GetCoalTypeLookupAsync() => Items((1, "тип"));

    public Task<IReadOnlyList<LookupItem>> GetCoalGradeLookupAsync() => Items((1, "марка"));

    public Task<IReadOnlyList<LookupItem>> GetBoundaryLookupAsync() => Items((1, "граница"));

    public Task<IReadOnlyList<LookupItem>> GetPillarLookupAsync() => Items((1, "целик"));

    public void Dispose() => Disposed = true;
}

public class ReserveBlocksViewModelTests
{
    private readonly FakeDialogs dialogs = new();
    private readonly DataFilter filter = new();
    private readonly FakeReserveBlocksData service = new();

    private ReserveBlocksViewModel Screen() => new(service, filter, dialogs);

    private static VariantRow Variant(int conditionId) => new(new SeamCond { ConditionId = conditionId }, 1, $"вариант {conditionId}");

    private static ReserveBlockLists Lists(int[] variants, params (int Id, string Name)[] parts) =>
        new(parts.Select(p => new LookupItem(p.Id, p.Name)).ToList(),
            new ObservableCollection<VariantRow>(variants.Select(Variant)),
            variants.Select(v => new LookupItem(v, $"вариант {v}")).ToList(),
            new[] { new BlockHoleItem(7, "A-1, комм.", 10, 12) });

    private static Block B(int id, string name) => new() { BlockId = id, BlockName = name };

    private void Keys(int seam, int interbed)
    {
        filter.SeamId = seam;
        filter.InterbedId = interbed;
    }

    /// <summary>Пласт 5, прослои 100 и 200: варианты 111 и 112 (в 200 — 211), детали 1 и 2 (у пласта 6 — 3).</summary>
    private void SeedLists()
    {
        service.Lists[(5, 100)] = Lists(new[] { 111, 112 }, (10, "1"), (11, "2"));
        service.Lists[(5, 200)] = Lists(new[] { 112, 211 }, (10, "1"), (11, "2"));
        service.Lists[(6, 100)] = Lists(new[] { 111 }, (30, "3"));
        service.Blocks[(1, 111, 10)] = new List<Block> { B(1, "1"), B(2, "2") };
        service.Blocks[(1, 111, 11)] = new List<Block> { B(3, "3") };
        service.Blocks[(1, 112, 10)] = new List<Block> { B(4, "4") };
    }

    [Fact]
    public void Screen_shows_the_data_seam_interbed_and_block_ribbon_groups()
    {
        Assert.Equal(RibbonGroups.Data | RibbonGroups.Seam | RibbonGroups.Interbed | RibbonGroups.Block, Screen().VisibleRibbonGroups);
        Assert.Equal("Блоки подсчёта запасов", Screen().Title);
    }

    [Fact]
    public void Each_grid_has_a_context_menu_with_refresh_only()
    {
        var screen = Screen();

        foreach (var menu in new[] { screen.BlocksMenu, screen.DetailsMenu, screen.UnitsMenu })
            Assert.Equal("Обновить", Assert.Single(menu.Items).Caption);
    }

    [Fact]
    public async Task First_load_selects_version_1_the_first_variant_and_part_and_shows_their_blocks()
    {
        Keys(5, 100);
        SeedLists();
        var screen = Screen();

        await screen.LoadAsync();

        Assert.Equal((1, 111, 10), (screen.SelectedVersionId, screen.SelectedVariantId, screen.SelectedPartId));
        Assert.Equal(new[] { "1", "2" }, screen.Blocks.Select(b => b.BlockName));
        Assert.Equal(1, screen.SelectedBlock.BlockId);
        Assert.Equal(new[] { 2, 1 }, screen.Versions.Select(v => v.Id));
        Assert.Equal(new[] { "A-1, комм." }, screen.HoleTexts);
        Assert.Equal("граница", screen.BoundaryLookup.Single().Name);
    }

    [Fact]
    public async Task Selecting_a_part_or_variant_or_version_shows_the_matching_blocks_without_reloading_the_lists()
    {
        Keys(5, 100);
        SeedLists();
        var screen = Screen();
        await screen.LoadAsync();

        screen.SelectedPartId = 11;
        await screen.Pending;
        Assert.Equal(new[] { "3" }, screen.Blocks.Select(b => b.BlockName));

        screen.SelectedPartId = 10;
        screen.SelectedVariantId = 112;
        await screen.Pending;
        Assert.Equal(new[] { "4" }, screen.Blocks.Select(b => b.BlockName));

        screen.SelectedVersionId = 2;
        await screen.Pending;
        Assert.Empty(screen.Blocks);
        Assert.Single(service.ListCalls);   // списки пласта и прослоя не перечитывались
    }

    [Fact]
    public async Task A_slow_stale_blocks_answer_does_not_overwrite_the_newer_one()
    {
        Keys(5, 100);
        SeedLists();
        var screen = Screen();
        await screen.LoadAsync();
        var slow = new TaskCompletionSource<ObservableCollection<Block>>();
        service.BlocksHandler = (_, _, part) => part == 11
            ? slow.Task
            : Task.FromResult(new ObservableCollection<Block> { B(9, "быстрый") });

        screen.SelectedPartId = 11;   // медленный ответ
        var first = screen.Pending;
        screen.SelectedPartId = 10;   // быстрый ответ
        await screen.Pending;
        slow.SetResult(new ObservableCollection<Block> { B(8, "медленный") });
        await first;

        Assert.Equal("быстрый", screen.Blocks.Single().BlockName);
    }

    [Fact]
    public async Task Changing_the_interbed_keeps_the_variant_if_it_remains_and_otherwise_takes_the_first_and_keeps_the_part()
    {
        Keys(5, 100);
        SeedLists();
        var screen = Screen();
        await screen.LoadAsync();
        screen.SelectedVariantId = 112;
        screen.SelectedPartId = 11;
        await screen.Pending;

        filter.InterbedId = 200;   // в прослое 200 вариант 112 есть
        await screen.Pending;
        Assert.Equal((112, 11), (screen.SelectedVariantId, screen.SelectedPartId));

        screen.SelectedVariantId = 211;
        await screen.Pending;
        filter.InterbedId = 100;   // в прослое 100 варианта 211 нет: первый (111)
        await screen.Pending;
        Assert.Equal((111, 11), (screen.SelectedVariantId, screen.SelectedPartId));
    }

    [Fact]
    public async Task Changing_the_seam_always_selects_the_first_part()
    {
        Keys(5, 100);
        SeedLists();
        var screen = Screen();
        await screen.LoadAsync();
        screen.SelectedPartId = 11;
        await screen.Pending;

        filter.SeamId = 6;
        await screen.Pending;

        Assert.Equal(30, screen.SelectedPartId);
        Assert.Equal(111, screen.SelectedVariantId);   // вариант 111 есть и у нового пласта
    }

    [Fact]
    public async Task Changing_only_the_line_or_borehole_does_not_reload()
    {
        Keys(5, 100);
        SeedLists();
        var screen = Screen();
        await screen.LoadAsync();

        filter.LineId = 3;
        filter.BoreholeId = 4;
        await screen.Pending;

        Assert.Single(service.ListCalls);
    }

    [Fact]
    public async Task Filter_changed_during_the_load_is_picked_up_by_the_same_load()
    {
        Keys(5, 100);
        SeedLists();
        service.OnLoadLists = (seam, _) =>
        {
            if (seam == 5)
                filter.SeamId = 6;   // лента выбрала другой пласт, пока шла загрузка
        };
        var screen = Screen();

        await screen.LoadAsync();

        Assert.Equal(new (int?, int?)[] { (5, 100), (6, 100) }, service.ListCalls);
        Assert.Equal(30, screen.SelectedPartId);
    }

    [Fact]
    public async Task Unsaved_changes_on_filter_change_are_saved_when_the_user_agrees_and_discarded_when_not()
    {
        Keys(5, 100);
        SeedLists();
        var screen = Screen();
        await screen.LoadAsync();
        service.Changed = true;
        dialogs.Answer(MessageBoxResult.Yes);

        filter.SeamId = 6;
        await screen.Pending;
        Assert.Equal(1, service.Saves);

        service.Changed = true;
        dialogs.Answer(MessageBoxResult.No);
        filter.InterbedId = 200;
        await screen.Pending;
        Assert.Equal(1, service.Saves);   // не сохраняли, данные перечитаны
        Assert.Equal(3, service.ListCalls.Count);
    }

    [Fact]
    public async Task Failed_save_on_filter_change_keeps_the_data_and_shows_the_errors()
    {
        Keys(5, 100);
        SeedLists();
        var screen = Screen();
        await screen.LoadAsync();
        service.Changed = true;
        service.SaveError = new DataValidationException(new[] { "Блок «1»: ошибка" });
        dialogs.Answer(MessageBoxResult.Yes);

        filter.SeamId = 6;
        await screen.Pending;

        Assert.Single(service.ListCalls);
        Assert.Contains("Блок «1»: ошибка", dialogs.Shown.Last().Text);
        Assert.True(screen.HasChanges);
    }

    [Fact]
    public async Task Choosing_a_hole_in_a_unit_row_applies_it_and_refreshes_the_grids()
    {
        Keys(5, 100);
        SeedLists();
        var screen = Screen();
        await screen.LoadAsync();
        var refreshes = 0;
        screen.RefreshRequested += (_, _) => refreshes++;
        var unit = new BlockUnit { HoleName = "A-1, комм." };

        screen.UnitHoleChanged(unit);

        Assert.Same(unit, Assert.Single(service.AppliedHoles));
        Assert.Equal(1, refreshes);
    }

    [Fact]
    public void Hole_changes_without_a_row_do_nothing()
    {
        var screen = Screen();

        screen.UnitHoleChanged(null);

        Assert.Empty(service.AppliedHoles);
    }

    [Fact]
    public async Task Saving_goes_through_the_service()
    {
        Keys(5, 100);
        SeedLists();
        var screen = Screen();
        await screen.LoadAsync();

        await screen.SaveAsync();

        Assert.Equal(1, service.Saves);
    }

    [Fact]
    public async Task Without_a_seam_nothing_is_selected_and_no_blocks_are_shown()
    {
        var screen = Screen();

        await screen.LoadAsync();

        Assert.Null(screen.SelectedVariantId);
        Assert.Null(screen.SelectedPartId);
        Assert.Empty(screen.Blocks);
        Assert.Null(screen.SelectedBlock);
    }

    [Fact]
    public async Task Dispose_unsubscribes_from_the_filter_and_disposes_the_service()
    {
        Keys(5, 100);
        SeedLists();
        var screen = Screen();
        await screen.LoadAsync();

        screen.Dispose();
        filter.SeamId = 6;

        Assert.True(service.Disposed);
        Assert.Single(service.ListCalls);
    }
}
