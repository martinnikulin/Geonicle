using System.Windows;
using Geonicle.Infrastructure;
using Geonicle.Wpf.Ribbon;
using Xunit;

namespace Geonicle.Wpf.Tests;

public class RibbonFilterViewModelTests
{
    private readonly FakeDialogs dialogs = new();
    private readonly FakeLookups lookups = FakeLookups.WithSampleData();
    private readonly DataFilter filter = new();
    private readonly RibbonFilterViewModel ribbon;

    public RibbonFilterViewModelTests()
    {
        ribbon = new RibbonFilterViewModel(lookups, filter, dialogs);
    }

    [Fact]
    public async Task Interbeds_are_loaded_and_the_first_one_is_selected()
    {
        await ribbon.RefreshAsync(RibbonGroups.Interbed);

        Assert.Equal(new[] { 100, 200 }, ribbon.Interbeds.Select(i => i.InterbedId));
        Assert.Equal(100, ribbon.SelectedInterbedId);
        Assert.Equal(100, filter.InterbedId);
    }

    [Fact]
    public async Task Seams_are_loaded_and_the_first_one_is_selected()
    {
        await ribbon.RefreshAsync(RibbonGroups.Seam);

        Assert.Equal(new[] { 5, 6 }, ribbon.Seams.Select(s => s.SeamId));
        Assert.Equal(5, ribbon.SelectedSeamId);
        Assert.Equal(5, filter.SeamId);
        Assert.Equal(0, lookups.InterbedCalls);   // другие группы не загружаются
    }

    [Fact]
    public async Task Choosing_a_seam_updates_the_shared_filter()
    {
        await ribbon.RefreshAsync(RibbonGroups.Seam);

        ribbon.SelectedSeamId = 6;

        Assert.Equal(6, filter.SeamId);
    }

    [Fact]
    public async Task Seam_refresh_keeps_the_selection_that_still_exists_and_otherwise_selects_the_first()
    {
        await ribbon.RefreshAsync(RibbonGroups.Seam);
        ribbon.SelectedSeamId = 6;

        await ribbon.RefreshAsync(RibbonGroups.Seam);
        Assert.Equal(6, ribbon.SelectedSeamId);

        lookups.Seams.RemoveAt(1);
        await ribbon.RefreshAsync(RibbonGroups.Seam);
        Assert.Equal(5, ribbon.SelectedSeamId);
    }

    [Fact]
    public async Task Lines_and_boreholes_are_loaded_and_the_first_items_are_selected()
    {
        await ribbon.RefreshAsync(RibbonGroups.LineBorehole);

        Assert.Equal(new[] { 10, 20 }, ribbon.Lines.Select(l => l.LineId));
        Assert.Equal(10, ribbon.SelectedLineId);
        Assert.Equal(new[] { "A", "B" }, ribbon.Boreholes.Select(b => b.HoleName));
        Assert.Equal(1, ribbon.SelectedBoreholeId);
        Assert.Equal((10, 1), (filter.LineId, filter.BoreholeId));
    }

    [Fact]
    public async Task Only_the_requested_groups_are_loaded()
    {
        await ribbon.RefreshAsync(RibbonGroups.Interbed);

        Assert.Equal(0, lookups.LineCalls);
        Assert.Empty(lookups.BoreholeCalls);
        Assert.Null(ribbon.SelectedLineId);
    }

    [Fact]
    public async Task Choosing_another_line_shows_its_boreholes_and_selects_the_first()
    {
        await ribbon.RefreshAsync(RibbonGroups.LineBorehole);

        ribbon.SelectedLineId = 20;
        await ribbon.Pending;

        Assert.Equal(new[] { "C" }, ribbon.Boreholes.Select(b => b.HoleName));
        Assert.Equal(3, ribbon.SelectedBoreholeId);
        Assert.Equal((20, 3), (filter.LineId, filter.BoreholeId));
    }

    [Fact]
    public async Task All_boreholes_ignores_the_line_and_selects_the_first_borehole()
    {
        await ribbon.RefreshAsync(RibbonGroups.LineBorehole);
        ribbon.SelectedLineId = 20;
        await ribbon.Pending;

        ribbon.AllBoreholes = true;
        await ribbon.Pending;

        Assert.Equal(new[] { "A", "B", "C" }, ribbon.Boreholes.Select(b => b.HoleName));
        Assert.Equal(1, ribbon.SelectedBoreholeId);
        Assert.True(filter.AllBoreholes);
        Assert.Equal((20, true), lookups.BoreholeCalls.Last());
    }

    [Fact]
    public async Task Unchecking_all_boreholes_returns_to_the_boreholes_of_the_line()
    {
        await ribbon.RefreshAsync(RibbonGroups.LineBorehole);
        ribbon.AllBoreholes = true;
        await ribbon.Pending;

        ribbon.AllBoreholes = false;
        await ribbon.Pending;

        Assert.Equal(new[] { "A", "B" }, ribbon.Boreholes.Select(b => b.HoleName));
        Assert.False(filter.AllBoreholes);
    }

    [Fact]
    public async Task Refresh_keeps_the_selection_that_still_exists()
    {
        await ribbon.RefreshAsync(RibbonGroups.Interbed | RibbonGroups.LineBorehole);
        ribbon.SelectedInterbedId = 200;
        ribbon.SelectedLineId = 20;
        await ribbon.Pending;

        await ribbon.RefreshAsync(RibbonGroups.Interbed | RibbonGroups.LineBorehole);

        Assert.Equal(200, ribbon.SelectedInterbedId);
        Assert.Equal(20, ribbon.SelectedLineId);
        Assert.Equal(3, ribbon.SelectedBoreholeId);
    }

    [Fact]
    public async Task Refresh_selects_the_first_item_when_the_selected_one_disappeared()
    {
        await ribbon.RefreshAsync(RibbonGroups.Interbed);
        ribbon.SelectedInterbedId = 200;
        lookups.Interbeds.RemoveAll(i => i.InterbedId == 200);

        await ribbon.RefreshAsync(RibbonGroups.Interbed);

        Assert.Equal(100, ribbon.SelectedInterbedId);
    }

    [Fact]
    public async Task Empty_lists_leave_nothing_selected()
    {
        lookups.Interbeds.Clear();
        lookups.Lines.Clear();

        await ribbon.RefreshAsync(RibbonGroups.Interbed | RibbonGroups.LineBorehole);

        Assert.Null(ribbon.SelectedInterbedId);
        Assert.Null(ribbon.SelectedLineId);
        Assert.Null(ribbon.SelectedBoreholeId);
        Assert.Empty(ribbon.Boreholes);
    }

    [Fact]
    public async Task A_slow_stale_load_does_not_overwrite_the_newer_one()
    {
        await ribbon.RefreshAsync(RibbonGroups.LineBorehole);
        var slow = new TaskCompletionSource<IReadOnlyList<LineBoreholeItem>>();
        var calls = 0;
        lookups.BoreholesHandler = (lineId, all) =>
            ++calls == 1
                ? slow.Task   // первая загрузка (линия 20) зависает
                : Task.FromResult<IReadOnlyList<LineBoreholeItem>>(new List<LineBoreholeItem> { new(1, 1, "A") });

        ribbon.SelectedLineId = 20;
        var firstLoad = ribbon.Pending;
        ribbon.SelectedLineId = 10;
        var secondLoad = ribbon.Pending;
        await secondLoad;
        slow.SetResult(new List<LineBoreholeItem> { new(3, 1, "C") });   // устаревший результат приходит позже
        await firstLoad;

        Assert.Equal(new[] { "A" }, ribbon.Boreholes.Select(b => b.HoleName));
        Assert.Equal(1, ribbon.SelectedBoreholeId);
    }

    [Fact]
    public async Task A_load_error_is_shown_and_does_not_throw()
    {
        lookups.Error = new InvalidOperationException("сервер недоступен");

        await ribbon.RefreshAsync(RibbonGroups.Interbed);

        var shown = Assert.Single(dialogs.Shown);
        Assert.Equal("Ошибка загрузки", shown.Caption);
        Assert.Equal("сервер недоступен", shown.Text);
        Assert.Equal(MessageBoxImage.Error, shown.Icon);
    }

    [Fact]
    public async Task A_borehole_load_error_after_choosing_a_line_is_shown()
    {
        await ribbon.RefreshAsync(RibbonGroups.LineBorehole);
        lookups.Error = new InvalidOperationException("обрыв связи");

        ribbon.SelectedLineId = 20;
        await ribbon.Pending;

        Assert.Equal("обрыв связи", Assert.Single(dialogs.Shown).Text);
    }
}
