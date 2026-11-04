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

    /// <summary>Если задан, выполняется при загрузке справочников (для проверки смены фильтра во время их загрузки).</summary>
    public Action OnLookup { get; set; }

    public int SeamNameUpdates { get; private set; }

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

    public void UpdateSeamNames() => SeamNameUpdates++;

    public List<(int? HoleSeamId, bool BySeam)> SampleViewCalls { get; } = new();

    public int SampleRefreshes { get; private set; }

    public void UpdateSampleView(int? holeSeamId, bool bySeam) => SampleViewCalls.Add((holeSeamId, bySeam));

    public Task RefreshSamplesAsync()
    {
        SampleRefreshes++;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<LookupItem>> GetAdIndexLookupAsync() => Items("притято по");

    public List<IReadOnlyList<Lithology>> Added { get; } = new();

    public List<(SeamDef Seam, IReadOnlyList<Lithology> Layers)> Redefined { get; } = new();

    /// <summary>Если задана, добавление и переопределение бросают её (ошибка, понятная пользователю).</summary>
    public HoleDataException EditError { get; set; }

    public Task<SeamDef> AddHoleSeamAsync(IReadOnlyList<Lithology> layers)
    {
        if (EditError != null) throw EditError;
        Added.Add(layers);
        return Task.FromResult(new SeamDef { HoleSeamId = 99 });
    }

    public Task RedefineHoleSeamAsync(SeamDef seam, IReadOnlyList<Lithology> layers)
    {
        if (EditError != null) throw EditError;
        Redefined.Add((seam, layers));
        return Task.CompletedTask;
    }

    private Task<IReadOnlyList<LookupItem>> Items(string name)
    {
        OnLookup?.Invoke();
        return Task.FromResult<IReadOnlyList<LookupItem>>(new[] { new LookupItem(1, name) });
    }

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

    // Глубины пластопересечения — вычисляемые, из слоёв-границ литологии.
    private static SeamDef Seam(int id, decimal from, decimal to) =>
        new()
        {
            HoleSeamId = id,
            DepthFromNavigation = new Lithology { DepthFrom = from },
            DepthToNavigation = new Lithology { DepthTo = to },
        };

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
    public async Task Filter_changed_while_lookups_load_on_the_first_open_is_picked_up()
    {
        // Первое открытие экрана: лента выбирает скважину и прослой, пока грузятся справочники.
        service.Data[(6, 100)] = Data(Seam(9, 1, 2));
        var changed = false;
        service.OnLookup = () =>
        {
            if (changed)
                return;
            changed = true;
            filter.InterbedId = 100;
            filter.BoreholeId = 6;
        };
        var screen = Screen();

        await screen.LoadAsync();

        Assert.Equal(9, screen.SelectedHoleSeam.HoleSeamId);
        Assert.Equal((6, 100), service.LoadCalls.Last());
    }

    [Fact]
    public async Task Seam_names_are_recalculated_after_saving()
    {
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        var screen = Screen();
        await screen.LoadAsync();

        await screen.SaveAsync();

        Assert.Equal(1, service.Saves);
        Assert.Equal(1, service.SeamNameUpdates);
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

    private static Lithology Layer(int id, decimal from, decimal to) => new() { LithologyId = id, DepthFrom = from, DepthTo = to };

    private async Task<LithologyViewModel> LoadedScreenWithSelection(bool sortedByDepth, params Lithology[] selected)
    {
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        service.Data[(5, 100)] = Data(Seam(1, 10, 12));
        var screen = Screen();
        await screen.LoadAsync();
        screen.LayerSelectionProvider = () => new LayerSelection(selected, sortedByDepth);
        return screen;
    }

    [Fact]
    public async Task Seam_menu_items_are_declared_in_the_lithology_menu()
    {
        var screen = await LoadedScreenWithSelection(true, Layer(1, 0, 5));

        Assert.Equal(
            new[] { "Обновить", "Добавить пластопересечение", "Переопределить пластопересечение" },
            screen.LithologyMenu.Items.Select(i => i.Caption));
        Assert.Same(screen.AddHoleSeamCommand, screen.LithologyMenu.Items[1].Command);
        Assert.Same(screen.RedefineHoleSeamCommand, screen.LithologyMenu.Items[2].Command);
    }

    [Fact]
    public async Task Seam_commands_are_available_for_selected_rows_when_sorted_by_depth_and_all_lithology_is_shown()
    {
        var screen = await LoadedScreenWithSelection(true, Layer(1, 0, 5));

        Assert.True(screen.CanAddHoleSeam);
        Assert.True(screen.CanRedefineHoleSeam);
    }

    [Fact]
    public async Task Seam_commands_are_unavailable_when_the_by_seam_checkbox_is_on()
    {
        var screen = await LoadedScreenWithSelection(true, Layer(1, 0, 5));

        screen.ShowByHoleSeam = true;

        Assert.False(screen.CanAddHoleSeam);
        Assert.False(screen.CanRedefineHoleSeam);
    }

    [Fact]
    public async Task Seam_commands_are_unavailable_without_selection_or_depth_sort()
    {
        var unsorted = await LoadedScreenWithSelection(false, Layer(1, 0, 5));
        Assert.False(unsorted.CanAddHoleSeam);

        var none = await LoadedScreenWithSelection(true);
        Assert.False(none.CanAddHoleSeam);

        var noGrid = Screen();
        await noGrid.LoadAsync();
        Assert.False(noGrid.CanAddHoleSeam);
    }

    [Fact]
    public async Task Redefine_needs_a_selected_seam_but_add_does_not()
    {
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        var screen = Screen();
        await screen.LoadAsync();   // пластопересечений нет
        screen.LayerSelectionProvider = () => new LayerSelection(new[] { Layer(1, 0, 5) }, true);

        Assert.True(screen.CanAddHoleSeam);
        Assert.False(screen.CanRedefineHoleSeam);
    }

    [Fact]
    public async Task Seam_commands_are_unavailable_without_a_borehole_or_interbed()
    {
        var screen = Screen();
        await screen.LoadAsync();   // лента ещё ничего не выбрала
        screen.LayerSelectionProvider = () => new LayerSelection(new[] { Layer(1, 0, 5) }, true);

        Assert.False(screen.CanAddHoleSeam);
    }

    [Fact]
    public async Task Adding_passes_the_selected_rows_selects_the_new_seam_and_refreshes_the_grids()
    {
        var selected = new[] { Layer(1, 0, 5), Layer(2, 5, 10) };
        var screen = await LoadedScreenWithSelection(true, selected);
        var refreshes = 0;
        screen.RefreshRequested += (_, _) => refreshes++;

        await screen.AddHoleSeamAsync();

        Assert.Equal(selected, service.Added.Single());
        Assert.Equal(99, screen.SelectedHoleSeam.HoleSeamId);
        Assert.Equal(1, refreshes);
    }

    [Fact]
    public async Task Redefining_passes_the_selected_seam_and_rows_and_refreshes_the_grids()
    {
        var selected = new[] { Layer(1, 0, 5) };
        var screen = await LoadedScreenWithSelection(true, selected);
        var seam = screen.SelectedHoleSeam;
        var refreshes = 0;
        screen.RefreshRequested += (_, _) => refreshes++;

        await screen.RedefineHoleSeamAsync();

        var call = Assert.Single(service.Redefined);
        Assert.Same(seam, call.Seam);
        Assert.Equal(selected, call.Layers);
        Assert.Equal(1, refreshes);
    }

    [Fact]
    public async Task Unavailable_commands_do_nothing()
    {
        var screen = await LoadedScreenWithSelection(false, Layer(1, 0, 5));

        await screen.AddHoleSeamAsync();
        await screen.RedefineHoleSeamAsync();

        Assert.Empty(service.Added);
        Assert.Empty(service.Redefined);
    }

    [Fact]
    public async Task Service_errors_are_shown_to_the_user_and_do_not_refresh()
    {
        var screen = await LoadedScreenWithSelection(true, Layer(1, 0, 5));
        service.EditError = new HoleDataException("Нет пласта без названия.");
        var refreshes = 0;
        screen.RefreshRequested += (_, _) => refreshes++;

        await screen.AddHoleSeamAsync();

        var shown = Assert.Single(dialogs.Shown);
        Assert.Equal("Нет пласта без названия.", shown.Text);
        Assert.Equal(MessageBoxImage.Warning, shown.Icon);
        Assert.Equal(0, refreshes);
    }

    [Fact]
    public async Task Samples_and_the_ad_index_lookup_are_loaded_with_the_screen()
    {
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        var samples = new ObservableCollection<SampleRow> { new(new SampleDef { Nsample = "A" }) };
        service.Data[(5, 100)] = new HoleData(new ObservableCollection<SeamDef>(), new ObservableCollection<Lithology>(), samples);
        var screen = Screen();

        await screen.LoadAsync();

        Assert.Same(samples, screen.Samples);
        Assert.Equal("притято по", screen.AdIndexLookup.Single().Name);
        Assert.Equal("Обновить", Assert.Single(screen.SamplesMenu.Items).Caption);   // меню грида проб: только «Обновить»
    }

    [Fact]
    public async Task Samples_follow_the_selected_seam_and_the_by_seam_mode()
    {
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        service.Data[(5, 100)] = Data(Seam(1, 10, 12), Seam(2, 20, 22));
        var screen = Screen();
        await screen.LoadAsync();
        service.SampleViewCalls.Clear();

        screen.SelectedHoleSeam = screen.HoleSeams[1];
        screen.ShowByHoleSeam = true;

        Assert.Equal(new (int?, bool)[] { (2, false), (2, true) }, service.SampleViewCalls);
    }

    [Fact]
    public async Task Samples_are_attached_to_the_first_seam_after_loading()
    {
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        service.Data[(5, 100)] = Data(Seam(1, 10, 12));
        var screen = Screen();

        await screen.LoadAsync();

        Assert.Contains((1, false), service.SampleViewCalls.Select(c => (c.HoleSeamId ?? 0, c.BySeam)));
    }

    [Fact]
    public async Task Samples_filter_is_off_for_the_whole_borehole_and_by_seam_depths_in_seam_mode()
    {
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        service.Data[(5, 100)] = Data(Seam(1, 10, 12), Seam(2, 20, 22));
        var screen = Screen();
        await screen.LoadAsync();
        Assert.Null(screen.SamplesFilterCriteria);

        screen.ShowByHoleSeam = true;
        var first = screen.SamplesFilterCriteria.ToString();
        // Пересечение, как в триггере БД: DepthFrom пробы < До пласта и DepthTo пробы > От пласта.
        Assert.Contains("[DepthFrom] Less 12", first);
        Assert.Contains("[DepthTo] Greater 10", first);
        // У литологии правило другое: слой целиком внутри пласта.
        screen.ShowByHoleSeam = true;
        Assert.Contains("[DepthFrom] GreaterOrEqual 10", screen.LithologyFilterCriteria.ToString());
        Assert.Contains("[DepthTo] LessOrEqual 12", screen.LithologyFilterCriteria.ToString());

        screen.ShowByHoleSeam = false;
        Assert.Null(screen.SamplesFilterCriteria);
    }

    [Fact]
    public async Task Samples_filter_changes_when_another_seam_is_selected()
    {
        // Раньше условие не менялось при смене пластопересечения, и грид не перефильтровывался.
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        service.Data[(5, 100)] = Data(Seam(1, 10, 12), Seam(2, 20, 22));
        var screen = Screen();
        await screen.LoadAsync();
        screen.ShowByHoleSeam = true;
        var changed = new List<string>();
        screen.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        var before = screen.SamplesFilterCriteria.ToString();

        screen.SelectedHoleSeam = screen.HoleSeams[1];

        Assert.NotEqual(before, screen.SamplesFilterCriteria.ToString());
        Assert.Contains("20", screen.SamplesFilterCriteria.ToString());
        Assert.Contains("22", screen.SamplesFilterCriteria.ToString());
        Assert.Contains(nameof(LithologyViewModel.SamplesFilterCriteria), changed);
    }

    [Fact]
    public async Task Samples_filter_shows_nothing_in_seam_mode_when_there_is_no_seam()
    {
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        var screen = Screen();
        await screen.LoadAsync();

        screen.ShowByHoleSeam = true;

        Assert.Contains("SampleId", screen.SamplesFilterCriteria.ToString());
    }

    [Fact]
    public async Task Accepted_is_editable_and_samples_cannot_be_added_only_in_the_matching_modes()
    {
        var screen = Screen();
        await screen.LoadAsync();
        var raised = new List<string>();
        screen.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        Assert.True(screen.AcceptedReadOnly);          // вся скважина: «Принято» только для чтения
        Assert.True(screen.CanAddOrDeleteSamples);     // добавлять и удалять пробы можно

        screen.ShowByHoleSeam = true;

        Assert.False(screen.AcceptedReadOnly);
        Assert.False(screen.CanAddOrDeleteSamples);
        Assert.Contains(nameof(LithologyViewModel.AcceptedReadOnly), raised);
        Assert.Contains(nameof(LithologyViewModel.CanAddOrDeleteSamples), raised);
    }

    [Fact]
    public async Task Saving_refreshes_the_sample_links_and_renumbers()
    {
        filter.BoreholeId = 5;
        filter.InterbedId = 100;
        service.Data[(5, 100)] = Data(Seam(1, 10, 12));
        var screen = Screen();
        await screen.LoadAsync();
        service.SampleViewCalls.Clear();

        await screen.SaveAsync();

        Assert.Equal(1, service.SampleRefreshes);
        Assert.Single(service.SampleViewCalls);
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
