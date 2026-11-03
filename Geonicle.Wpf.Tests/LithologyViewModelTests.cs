using System.Collections.ObjectModel;
using System.Windows;
using Data.Models;
using DevExpress.Data.Filtering;
using Geonicle.Data.Services;
using Geonicle.Infrastructure;
using Geonicle.Validation;
using Geonicle.Wpf.Ribbon;
using Geonicle.Wpf.ViewModels;
using Xunit;

namespace Geonicle.Wpf.Tests;

/// <summary>Заглушка сервиса экрана «Данные по скважинам»: данные по ключу (скважина, прослой), вызовы запоминаются.</summary>
public sealed class FakeHoleData : IHoleDataService
{
    public Dictionary<(int?, int?), HoleData> Data { get; } = new();

    public List<(int? BoreholeId, int? InterbedId)> LoadCalls { get; } = new();

    public bool Changed { get; set; }

    public int Saves { get; private set; }

    public bool Disposed { get; private set; }

    public Exception SaveError { get; set; }

    /// <summary>Если задан, выполняется внутри загрузки (для проверки смены фильтра во время загрузки).</summary>
    public Action<int?, int?> OnLoad { get; set; }

    public bool HasChanges => Changed;

    public Task<HoleData> LoadAsync(int? boreholeId, int? interbedId)
    {
        LoadCalls.Add((boreholeId, interbedId));
        OnLoad?.Invoke(boreholeId, interbedId);
        Changed = false;
        return Task.FromResult(Data.TryGetValue((boreholeId, interbedId), out var data)
            ? data
            : new HoleData(new ObservableCollection<SeamDef>(), new ObservableCollection<Lithology>()));
    }

    public Task SaveAsync()
    {
        if (SaveError != null)
            throw SaveError;
        Saves++;
        Changed = false;
        return Task.CompletedTask;
    }

    private static Task<IReadOnlyList<LookupItem>> Items(string name) =>
        Task.FromResult<IReadOnlyList<LookupItem>>(new[] { new LookupItem(1, name) });

    public Task<IReadOnlyList<LookupItem>> GetSeamLookupAsync() => Items("пласт");

    public Task<IReadOnlyList<LookupItem>> GetCoalGradeLookupAsync() => Items("марка");

    public Task<IReadOnlyList<LookupItem>> GetCoalTypeLookupAsync() => Items("тип");

    public Task<IReadOnlyList<LookupItem>> GetFormationLookupAsync() => Items("свита");

    public Task<IReadOnlyList<LookupItem>> GetRockLookupAsync() => Items("порода");

    public void Dispose() => Disposed = true;
}

public class LithologyViewModelTests
{
    private readonly FakeDialogs dialogs = new();
    private readonly DataFilter filter = new();
    private readonly FakeHoleData service = new();

    private LithologyViewModel Screen() => new(service, filter, dialogs);

    private static HoleData Data(params SeamDef[] seams) =>
        new(new ObservableCollection<SeamDef>(seams), new ObservableCollection<Lithology>());

    private static SeamDef Seam(int id, decimal from, decimal to) =>
        new() { HoleSeamId = id, DepthFrom = from, DepthTo = to };

    [Fact]
    public async Task Loads_data_for_the_filter_borehole_and_interbed_with_lookups()
    {
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        service.Data[(5, 100)] = Data(Seam(1, 10, 12), Seam(2, 20, 22));
        var screen = Screen();

        await screen.LoadAsync();

        Assert.Equal((5, 100), service.LoadCalls.Single());
        Assert.Equal(2, screen.HoleSeams.Count);
        Assert.Equal(1, screen.SelectedHoleSeam.HoleSeamId);
        Assert.Equal("пласт", screen.SeamLookup.Single().Name);
        Assert.Equal("порода", screen.RockLookup.Single().Name);
        Assert.Equal(HoleSeamKb.Values, screen.KbValues);
    }

    [Fact]
    public void Screen_shows_all_three_ribbon_groups()
    {
        Assert.Equal(RibbonGroups.Data | RibbonGroups.Interbed | RibbonGroups.LineBorehole, Screen().VisibleRibbonGroups);
    }

    [Fact]
    public async Task Changing_the_ribbon_borehole_reloads_the_data()
    {
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        service.Data[(6, 100)] = Data(Seam(9, 1, 2));
        var screen = Screen();
        await screen.LoadAsync();

        filter.BoreholeId = 6;
        await screen.Pending;

        Assert.Equal(2, service.LoadCalls.Count);
        Assert.Equal((6, 100), service.LoadCalls[1]);
        Assert.Equal(9, screen.SelectedHoleSeam.HoleSeamId);
    }

    [Fact]
    public async Task Changing_the_line_does_not_reload_when_borehole_and_interbed_stay_the_same()
    {
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        var screen = Screen();
        await screen.LoadAsync();

        filter.LineId = 3;
        filter.AllBoreholes = true;
        await screen.Pending;

        Assert.Single(service.LoadCalls);
    }

    [Fact]
    public void Filter_changes_before_the_first_load_do_not_load_anything()
    {
        var screen = Screen();
        filter.BoreholeId = 5;
        filter.InterbedId = 100;

        Assert.Empty(service.LoadCalls);
    }

    [Fact]
    public async Task Filter_changed_during_the_load_is_picked_up_by_the_same_load()
    {
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        service.Data[(6, 100)] = Data(Seam(9, 1, 2));
        service.OnLoad = (borehole, _) =>
        {
            if (borehole == 5)
                filter.BoreholeId = 6;   // лента выбрала другую скважину, пока шла загрузка
        };
        var screen = Screen();

        await screen.LoadAsync();

        Assert.Equal(new (int?, int?)[] { (5, 100), (6, 100) }, service.LoadCalls);
        Assert.Equal(9, screen.SelectedHoleSeam.HoleSeamId);
    }

    [Fact]
    public async Task Unsaved_changes_on_filter_change_are_saved_when_the_user_agrees()
    {
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        var screen = Screen();
        await screen.LoadAsync();
        service.Changed = true;
        dialogs.Answer(MessageBoxResult.Yes);

        filter.BoreholeId = 6;
        await screen.Pending;

        Assert.Equal(1, service.Saves);
        Assert.Equal(2, service.LoadCalls.Count);
    }

    [Fact]
    public async Task Unsaved_changes_on_filter_change_are_discarded_when_the_user_declines()
    {
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        var screen = Screen();
        await screen.LoadAsync();
        service.Changed = true;
        dialogs.Answer(MessageBoxResult.No);

        filter.BoreholeId = 6;
        await screen.Pending;

        Assert.Equal(0, service.Saves);
        Assert.Equal(2, service.LoadCalls.Count);
        Assert.Equal(MessageBoxButton.YesNo, dialogs.Shown.Single().Buttons);
    }

    [Fact]
    public async Task Failed_save_on_filter_change_keeps_the_data_and_shows_the_errors()
    {
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        var screen = Screen();
        await screen.LoadAsync();
        service.Changed = true;
        service.SaveError = new DataValidationException(new[] { "Литология 1–2: ошибка" });
        dialogs.Answer(MessageBoxResult.Yes);

        filter.BoreholeId = 6;
        await screen.Pending;

        Assert.Single(service.LoadCalls);
        Assert.Contains("Литология 1–2: ошибка", dialogs.Shown.Last().Text);
        Assert.Equal(MessageBoxImage.Warning, dialogs.Shown.Last().Icon);
        Assert.True(screen.HasChanges);
    }

    [Fact]
    public async Task Lithology_filter_is_off_by_default_and_follows_the_selected_seam_when_on()
    {
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        service.Data[(5, 100)] = Data(Seam(1, 10, 12), Seam(2, 20, 22));
        var screen = Screen();
        await screen.LoadAsync();
        Assert.Null(screen.LithologyFilterCriteria);

        screen.ShowByHoleSeam = true;
        var first = screen.LithologyFilterCriteria.ToString();
        screen.SelectedHoleSeam = screen.HoleSeams[1];
        var second = screen.LithologyFilterCriteria.ToString();

        Assert.NotNull(first);
        Assert.NotEqual(first, second);
        Assert.Contains("10", first);
        Assert.Contains("12", first);
        Assert.Contains("20", second);
        Assert.Contains("22", second);

        screen.ShowByHoleSeam = false;
        Assert.Null(screen.LithologyFilterCriteria);
    }

    [Fact]
    public async Task Lithology_filter_shows_nothing_when_there_is_no_seam()
    {
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        var screen = Screen();
        await screen.LoadAsync();

        screen.ShowByHoleSeam = true;

        Assert.NotNull(screen.LithologyFilterCriteria);
        Assert.Contains("LithologyId", screen.LithologyFilterCriteria.ToString());
    }

    [Fact]
    public async Task Selected_seam_is_kept_on_reload_when_it_still_exists()
    {
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        service.Data[(5, 100)] = Data(Seam(1, 10, 12), Seam(2, 20, 22));
        var screen = Screen();
        await screen.LoadAsync();
        screen.SelectedHoleSeam = screen.HoleSeams[1];
        service.Data[(5, 100)] = Data(Seam(1, 10, 12), Seam(2, 20, 22));

        await screen.RefreshAsync();

        Assert.Equal(2, screen.SelectedHoleSeam.HoleSeamId);
    }

    [Fact]
    public async Task Dispose_unsubscribes_from_the_filter_and_disposes_the_service()
    {
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        var screen = Screen();
        await screen.LoadAsync();

        screen.Dispose();
        filter.BoreholeId = 6;

        Assert.True(service.Disposed);
        Assert.Single(service.LoadCalls);
    }
}
