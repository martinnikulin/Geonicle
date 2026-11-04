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

/// <summary>Заглушка сервиса экрана «Пластопересечения»: данные по ключу (пласт, прослой) и по пластопересечению.</summary>
public sealed class FakeSeamIntersectionsData : ISeamIntersectionsService
{
    public Dictionary<(int?, int?), SeamIntersectionsData> Data { get; } = new();

    public Dictionary<int, SeamIntersectionDetails> Details { get; } = new();

    public List<(int? SeamId, int? InterbedId)> LoadCalls { get; } = new();

    public List<int?> DetailsCalls { get; } = new();

    public bool Changed { get; set; }

    public int Saves { get; private set; }

    public int AfterSaveRefreshes { get; private set; }

    public int ValueRefreshes { get; private set; }

    public bool Disposed { get; private set; }

    public Exception SaveError { get; set; }

    /// <summary>Если задан, выполняется внутри загрузки (для проверки смены фильтра во время загрузки).</summary>
    public Action<int?, int?> OnLoad { get; set; }

    /// <summary>Если задан, перехватывает загрузку литологии и проб (для проверки гонок).</summary>
    public Func<SeamDef, Task<SeamIntersectionDetails>> DetailsHandler { get; set; }

    public bool HasChanges => Changed;

    public Task<SeamIntersectionsData> LoadAsync(int? seamId, int? interbedId)
    {
        LoadCalls.Add((seamId, interbedId));
        OnLoad?.Invoke(seamId, interbedId);
        Changed = false;
        return Task.FromResult(Data.TryGetValue((seamId, interbedId), out var data)
            ? data
            : new SeamIntersectionsData(new ObservableCollection<SeamDef>(), new ObservableCollection<Average>()));
    }

    public Task<SeamIntersectionDetails> LoadDetailsAsync(SeamDef seam)
    {
        DetailsCalls.Add(seam?.HoleSeamId);
        if (DetailsHandler != null && seam != null)
            return DetailsHandler(seam);
        return Task.FromResult(seam != null && Details.TryGetValue(seam.HoleSeamId, out var details)
            ? details
            : new SeamIntersectionDetails(new ObservableCollection<Lithology>(), new ObservableCollection<SampleRow>()));
    }

    public Task SaveAsync()
    {
        if (SaveError != null)
            throw SaveError;
        Saves++;
        Changed = false;
        return Task.CompletedTask;
    }

    public Task RefreshAfterSaveAsync()
    {
        AfterSaveRefreshes++;
        return Task.CompletedTask;
    }

    public Task RefreshSampleValuesAsync()
    {
        ValueRefreshes++;
        return Task.CompletedTask;
    }

    private static Task<IReadOnlyList<LookupItem>> Items(string name) =>
        Task.FromResult<IReadOnlyList<LookupItem>>(new[] { new LookupItem(1, name) });

    public Task<IReadOnlyList<LookupItem>> GetCoalGradeLookupAsync() => Items("марка");

    public Task<IReadOnlyList<LookupItem>> GetCoalTypeLookupAsync() => Items("тип");

    public Task<IReadOnlyList<LookupItem>> GetAdIndexLookupAsync() => Items("притято по");

    public Task<IReadOnlyList<LookupItem>> GetAssayVarLookupAsync() => Items("показатель");

    public Task<IReadOnlyList<LookupItem>> GetRockLookupAsync() => Items("порода");

    public void Dispose() => Disposed = true;
}

public class SeamIntersectionsViewModelTests
{
    private readonly FakeDialogs dialogs = new();
    private readonly DataFilter filter = new();
    private readonly FakeSeamIntersectionsData service = new();
    private readonly FakeAssayWindows windows = new();

    private SeamIntersectionsViewModel Screen() => new(service, filter, dialogs, windows);

    private static SeamDef Seam(int id) => new() { HoleSeamId = id };

    private static SeamIntersectionsData Data(params SeamDef[] seams) =>
        new(new ObservableCollection<SeamDef>(seams), new ObservableCollection<Average>());

    private static SeamIntersectionDetails Details(params string[] samples) =>
        new(new ObservableCollection<Lithology>(),
            new ObservableCollection<SampleRow>(samples.Select(s => new SampleRow(new SampleDef { SampleId = 1, Nsample = s }))));

    private void Keys(int seam, int interbed)
    {
        filter.SeamId = seam;
        filter.InterbedId = interbed;
    }

    [Fact]
    public void Screen_shows_the_data_seam_and_interbed_ribbon_groups()
    {
        Assert.Equal(RibbonGroups.Data | RibbonGroups.Seam | RibbonGroups.Interbed, Screen().VisibleRibbonGroups);
        Assert.Equal("Пластопересечения", Screen().Title);
    }

    [Fact]
    public async Task Loads_the_data_of_the_ribbon_seam_and_interbed_and_the_details_of_the_first_row()
    {
        Keys(5, 100);
        service.Data[(5, 100)] = Data(Seam(1), Seam(2));
        service.Details[1] = Details("P1");
        var screen = Screen();

        await screen.LoadAsync();

        Assert.Equal((5, 100), service.LoadCalls.Single());
        Assert.Equal(2, screen.HoleSeams.Count);
        Assert.Equal(1, screen.SelectedHoleSeam.HoleSeamId);
        Assert.Equal("P1", screen.Samples.Single().Nsample);
        Assert.Equal("порода", screen.RockLookup.Single().Name);
        Assert.Equal("показатель", screen.AssayVarLookup.Single().Name);
        Assert.Equal("притято по", screen.AdIndexLookup.Single().Name);
    }

    [Fact]
    public void Each_grid_has_a_context_menu_with_refresh_only()
    {
        var screen = Screen();

        foreach (var menu in new[] { screen.HoleSeamsMenu, screen.AveragesMenu, screen.LithologyMenu, screen.SamplesMenu })
            Assert.Equal("Обновить", Assert.Single(menu.Items).Caption);
    }

    [Fact]
    public async Task Selecting_another_seam_intersection_loads_its_details()
    {
        Keys(5, 100);
        service.Data[(5, 100)] = Data(Seam(1), Seam(2));
        service.Details[2] = Details("P2");
        var screen = Screen();
        await screen.LoadAsync();

        screen.SelectedHoleSeam = screen.HoleSeams[1];
        await screen.Pending;

        Assert.Equal(new int?[] { 1, 2 }, service.DetailsCalls);
        Assert.Equal("P2", screen.Samples.Single().Nsample);
    }

    [Fact]
    public async Task A_slow_stale_details_answer_does_not_overwrite_the_newer_one()
    {
        Keys(5, 100);
        service.Data[(5, 100)] = Data(Seam(1), Seam(2));
        var screen = Screen();
        await screen.LoadAsync();
        var slow = new TaskCompletionSource<SeamIntersectionDetails>();
        service.DetailsHandler = seam => seam.HoleSeamId == 1 ? slow.Task : Task.FromResult(Details("быстрый"));

        screen.SelectedHoleSeam = screen.HoleSeams[0];   // медленный ответ
        var first = screen.Pending;
        screen.SelectedHoleSeam = screen.HoleSeams[1];   // быстрый ответ
        await screen.Pending;
        slow.SetResult(Details("медленный"));
        await first;

        Assert.Equal("быстрый", screen.Samples.Single().Nsample);
    }

    [Fact]
    public async Task Changing_the_ribbon_seam_or_interbed_reloads_the_data()
    {
        Keys(5, 100);
        service.Data[(6, 100)] = Data(Seam(9));
        service.Data[(6, 200)] = Data(Seam(8));
        var screen = Screen();
        await screen.LoadAsync();

        filter.SeamId = 6;
        await screen.Pending;
        Assert.Equal(9, screen.SelectedHoleSeam.HoleSeamId);

        filter.InterbedId = 200;
        await screen.Pending;
        Assert.Equal(new (int?, int?)[] { (5, 100), (6, 100), (6, 200) }, service.LoadCalls);
        Assert.Equal(8, screen.SelectedHoleSeam.HoleSeamId);
    }

    [Fact]
    public async Task Changing_only_the_line_or_borehole_does_not_reload()
    {
        Keys(5, 100);
        var screen = Screen();
        await screen.LoadAsync();

        filter.LineId = 3;
        filter.BoreholeId = 4;
        await screen.Pending;

        Assert.Single(service.LoadCalls);
    }

    [Fact]
    public void Filter_changes_before_the_first_load_do_not_load_anything()
    {
        Screen();
        Keys(5, 100);

        Assert.Empty(service.LoadCalls);
    }

    [Fact]
    public async Task Filter_changed_during_the_load_is_picked_up_by_the_same_load()
    {
        Keys(5, 100);
        service.Data[(6, 100)] = Data(Seam(9));
        service.OnLoad = (seam, _) =>
        {
            if (seam == 5)
                filter.SeamId = 6;   // лента выбрала другой пласт, пока шла загрузка
        };
        var screen = Screen();

        await screen.LoadAsync();

        Assert.Equal(new (int?, int?)[] { (5, 100), (6, 100) }, service.LoadCalls);
        Assert.Equal(9, screen.SelectedHoleSeam.HoleSeamId);
    }

    [Fact]
    public async Task Unsaved_changes_on_filter_change_are_saved_when_the_user_agrees()
    {
        Keys(5, 100);
        var screen = Screen();
        await screen.LoadAsync();
        service.Changed = true;
        dialogs.Answer(MessageBoxResult.Yes);

        filter.SeamId = 6;
        await screen.Pending;

        Assert.Equal(1, service.Saves);
        Assert.Equal(2, service.LoadCalls.Count);
    }

    [Fact]
    public async Task Unsaved_changes_on_filter_change_are_discarded_when_the_user_declines()
    {
        Keys(5, 100);
        var screen = Screen();
        await screen.LoadAsync();
        service.Changed = true;
        dialogs.Answer(MessageBoxResult.No);

        filter.SeamId = 6;
        await screen.Pending;

        Assert.Equal(0, service.Saves);
        Assert.Equal(2, service.LoadCalls.Count);
    }

    [Fact]
    public async Task Failed_save_on_filter_change_keeps_the_data_and_shows_the_errors()
    {
        Keys(5, 100);
        var screen = Screen();
        await screen.LoadAsync();
        service.Changed = true;
        service.SaveError = new DataValidationException(new[] { "Среднее: ошибка" });
        dialogs.Answer(MessageBoxResult.Yes);

        filter.SeamId = 6;
        await screen.Pending;

        Assert.Single(service.LoadCalls);
        Assert.Contains("Среднее: ошибка", dialogs.Shown.Last().Text);
        Assert.True(screen.HasChanges);
    }

    [Fact]
    public async Task Saving_refreshes_the_sample_links_and_values()
    {
        Keys(5, 100);
        var screen = Screen();
        await screen.LoadAsync();

        await screen.SaveAsync();

        Assert.Equal(1, service.Saves);
        Assert.Equal(1, service.AfterSaveRefreshes);
    }

    [Fact]
    public void Opening_assays_of_a_saved_sample_opens_its_window()
    {
        var screen = Screen();

        screen.OpenAssays(new SampleRow(new SampleDef { SampleId = 42, Nsample = "4117 сн.1" }));

        var opened = Assert.Single(windows.Opened);
        Assert.Equal((42, "Анализы: проба № 4117 сн.1"), (opened.SampleId, opened.Title));
    }

    [Fact]
    public void Unsaved_sample_cannot_have_assays_yet()
    {
        var screen = Screen();

        screen.OpenAssays(new SampleRow());

        Assert.Empty(windows.Opened);
        Assert.Contains("Сначала сохраните пробу", Assert.Single(dialogs.Shown).Text);
    }

    [Fact]
    public async Task Saved_assays_refresh_only_the_ad_values_and_the_grids()
    {
        var screen = Screen();
        screen.OpenAssays(new SampleRow(new SampleDef { SampleId = 42 }));
        var refreshes = 0;
        screen.RefreshRequested += (_, _) => refreshes++;

        windows.Opened.Single().Saved();   // «ОК» в окне анализов
        await screen.Pending;

        Assert.Equal(1, service.ValueRefreshes);
        Assert.Equal(0, service.AfterSaveRefreshes);   // связи и несохранённые правки не трогаются
        Assert.Equal(1, refreshes);
    }

    [Fact]
    public async Task Dispose_unsubscribes_closes_the_assay_windows_and_disposes_the_service()
    {
        Keys(5, 100);
        var screen = Screen();
        await screen.LoadAsync();

        screen.Dispose();
        filter.SeamId = 6;

        Assert.True(service.Disposed);
        Assert.Equal(1, windows.CloseAllCalls);
        Assert.Single(service.LoadCalls);
    }
}
