using System.Windows;
using Geonicle.Validation;
using Geonicle.Wpf.Ribbon;
using Geonicle.Wpf.Services;
using Geonicle.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Geonicle.Wpf.Tests;

public class MainViewModelTests
{
    private readonly FakeDialogs dialogs = new();
    private readonly FakeLookups lookups = FakeLookups.WithSampleData();
    private readonly MainViewModel main;

    public MainViewModelTests()
    {
        var provider = new ServiceCollection()
            .AddSingleton<IUserDialogs>(dialogs)
            .AddTransient<TestScreen>()
            .AddTransient<OtherScreen>()
            .AddTransient<InterbedScreen>()
            .AddTransient<AllGroupsScreen>()
            .AddTransient<SeamScreen>()
            .AddTransient<BlockScreen>()
            .BuildServiceProvider();
        var filters = new RibbonFilterViewModel(lookups, new DataFilter(), dialogs);
        main = new MainViewModel(provider, dialogs, filters);
    }

    /// <summary>Открывает первый экран, загружает его и (по желанию) помечает правки несохранёнными.</summary>
    private async Task<TestScreen> OpenFirstScreen(bool withChanges = false)
    {
        Assert.True(await main.NavigateAsync(typeof(TestScreen)));
        var screen = (TestScreen)main.CurrentViewModel;
        await screen.LoadAsync();
        screen.Pending = withChanges;
        return screen;
    }

    [Fact]
    public async Task First_navigation_creates_the_requested_screen()
    {
        Assert.True(await main.NavigateAsync(typeof(TestScreen)));

        Assert.IsType<TestScreen>(main.CurrentViewModel);
        Assert.Empty(dialogs.Shown);
    }

    [Fact]
    public async Task Navigation_without_changes_switches_screens_and_disposes_the_previous_one()
    {
        var first = await OpenFirstScreen();

        Assert.True(await main.NavigateAsync(typeof(OtherScreen)));

        Assert.IsType<OtherScreen>(main.CurrentViewModel);
        Assert.True(first.Disposed);
        Assert.Empty(dialogs.Shown);
    }

    [Fact]
    public async Task Cancel_keeps_the_current_screen()
    {
        var first = await OpenFirstScreen(withChanges: true);
        dialogs.Answer(MessageBoxResult.Cancel);

        Assert.False(await main.NavigateAsync(typeof(OtherScreen)));

        Assert.Same(first, main.CurrentViewModel);
        Assert.False(first.Disposed);
        Assert.Equal(MessageBoxButton.YesNoCancel, Assert.Single(dialogs.Shown).Buttons);
    }

    [Fact]
    public async Task No_discards_the_changes_and_navigates_without_saving()
    {
        var first = await OpenFirstScreen(withChanges: true);
        dialogs.Answer(MessageBoxResult.No);

        Assert.True(await main.NavigateAsync(typeof(OtherScreen)));

        Assert.Equal(0, first.Saves);
        Assert.True(first.Disposed);
        Assert.IsType<OtherScreen>(main.CurrentViewModel);
    }

    [Fact]
    public async Task Yes_saves_and_then_navigates()
    {
        var first = await OpenFirstScreen(withChanges: true);
        dialogs.Answer(MessageBoxResult.Yes);

        Assert.True(await main.NavigateAsync(typeof(OtherScreen)));

        Assert.Equal(1, first.Saves);
        Assert.True(first.Disposed);
    }

    [Fact]
    public async Task Yes_with_a_failing_save_stays_on_the_screen_and_shows_the_error()
    {
        var first = await OpenFirstScreen(withChanges: true);
        first.SaveError = new InvalidOperationException("ограничение БД");
        dialogs.Answer(MessageBoxResult.Yes);

        Assert.False(await main.NavigateAsync(typeof(OtherScreen)));

        Assert.Same(first, main.CurrentViewModel);
        Assert.False(first.Disposed);
        Assert.Contains(dialogs.Shown, m => m.Caption == "Ошибка сохранения");
    }

    [Fact]
    public async Task Save_without_errors_shows_nothing()
    {
        var screen = await OpenFirstScreen(withChanges: true);

        await main.SaveAsync();

        Assert.Equal(1, screen.Saves);
        Assert.Empty(dialogs.Shown);
    }

    [Fact]
    public async Task Validation_errors_are_shown_as_a_list()
    {
        var screen = await OpenFirstScreen(withChanges: true);
        screen.SaveError = new DataValidationException(new[] { "Скважина «A-1»: Глубина скважины не может быть отрицательной." });

        await main.SaveAsync();

        var shown = Assert.Single(dialogs.Shown);
        Assert.Equal("Проверка данных", shown.Caption);
        Assert.Equal(MessageBoxImage.Warning, shown.Icon);
        Assert.StartsWith("Нельзя сохранить: есть ошибки в данных.", shown.Text);
        Assert.Contains("• Скважина «A-1»: Глубина скважины не может быть отрицательной.", shown.Text);
    }

    [Fact]
    public async Task Other_save_errors_show_the_inner_message_and_a_hint()
    {
        var screen = await OpenFirstScreen(withChanges: true);
        screen.SaveError = new InvalidOperationException("общая ошибка", new InvalidOperationException("нарушение уникальности"));

        await main.SaveAsync();

        var shown = Assert.Single(dialogs.Shown);
        Assert.Equal("Ошибка сохранения", shown.Caption);
        Assert.Equal(MessageBoxImage.Error, shown.Icon);
        Assert.Contains("нарушение уникальности", shown.Text);
        Assert.Contains("нажмите «Обновить»", shown.Text);
    }

    [Fact]
    public async Task Save_and_refresh_without_a_screen_do_nothing()
    {
        await main.SaveAsync();
        await main.RefreshAsync();

        Assert.Empty(dialogs.Shown);
    }

    [Fact]
    public async Task Refresh_reloads_the_current_screen()
    {
        var screen = await OpenFirstScreen();

        await main.RefreshAsync();

        Assert.Equal(2, screen.Loads);
    }

    // ---- группы ленты

    [Fact]
    public void No_group_is_visible_before_any_screen_is_opened()
    {
        Assert.False(main.IsDataGroupVisible);
        Assert.False(main.IsInterbedGroupVisible);
        Assert.False(main.IsLineBoreholeGroupVisible);
        Assert.False(main.IsSeamGroupVisible);
        Assert.False(main.IsBlockGroupVisible);
    }

    [Fact]
    public async Task A_screen_with_grids_shows_only_the_Data_group()
    {
        await main.NavigateAsync(typeof(TestScreen));

        Assert.True(main.IsDataGroupVisible);
        Assert.False(main.IsInterbedGroupVisible);
        Assert.False(main.IsLineBoreholeGroupVisible);
    }

    [Fact]
    public async Task Groups_follow_the_open_screen()
    {
        await main.NavigateAsync(typeof(InterbedScreen));
        Assert.True(main.IsDataGroupVisible && main.IsInterbedGroupVisible && !main.IsLineBoreholeGroupVisible);

        await main.NavigateAsync(typeof(AllGroupsScreen));
        Assert.True(main.IsDataGroupVisible && main.IsInterbedGroupVisible && main.IsLineBoreholeGroupVisible);

        await main.NavigateAsync(typeof(TestScreen));
        Assert.True(main.IsDataGroupVisible && !main.IsInterbedGroupVisible && !main.IsLineBoreholeGroupVisible);
    }

    [Fact]
    public async Task The_seam_group_is_visible_only_on_screens_that_ask_for_it()
    {
        await main.NavigateAsync(typeof(SeamScreen));
        Assert.True(main.IsDataGroupVisible && main.IsSeamGroupVisible && main.IsInterbedGroupVisible && !main.IsLineBoreholeGroupVisible);

        await main.NavigateAsync(typeof(AllGroupsScreen));
        Assert.False(main.IsSeamGroupVisible);
    }

    [Fact]
    public async Task The_block_group_is_visible_only_on_screens_that_ask_for_it_and_its_lists_are_not_loaded_by_the_ribbon()
    {
        await main.NavigateAsync(typeof(BlockScreen));
        Assert.True(main.IsDataGroupVisible && main.IsSeamGroupVisible && main.IsInterbedGroupVisible && main.IsBlockGroupVisible);
        Assert.Equal(1, lookups.SeamCalls);       // пласт и прослой ленты загружаются,
        Assert.Equal(1, lookups.InterbedCalls);
        Assert.Equal(0, lookups.LineCalls);       // а списки блока принадлежат экрану

        await main.NavigateAsync(typeof(SeamScreen));
        Assert.False(main.IsBlockGroupVisible);
    }

    [Fact]
    public async Task Seams_and_interbeds_are_loaded_for_the_seam_screen_and_the_first_seam_is_selected()
    {
        await main.NavigateAsync(typeof(SeamScreen));

        Assert.Equal(1, lookups.SeamCalls);
        Assert.Equal(1, lookups.InterbedCalls);
        Assert.Equal(0, lookups.LineCalls);
        Assert.Equal(5, main.Filters.SelectedSeamId);
    }

    [Fact]
    public async Task Seams_are_not_loaded_for_other_screens()
    {
        await main.NavigateAsync(typeof(AllGroupsScreen));

        Assert.Equal(0, lookups.SeamCalls);
    }

    [Fact]
    public async Task Navigation_notifies_the_ribbon_about_group_changes()
    {
        var changed = new List<string>();
        main.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        await main.NavigateAsync(typeof(AllGroupsScreen));

        Assert.Contains(nameof(MainViewModel.IsDataGroupVisible), changed);
        Assert.Contains(nameof(MainViewModel.IsInterbedGroupVisible), changed);
        Assert.Contains(nameof(MainViewModel.IsLineBoreholeGroupVisible), changed);
        Assert.Contains(nameof(MainViewModel.IsSeamGroupVisible), changed);
        Assert.Contains(nameof(MainViewModel.IsBlockGroupVisible), changed);
    }

    [Fact]
    public async Task Lists_are_refreshed_when_a_screen_that_uses_them_is_opened()
    {
        await main.NavigateAsync(typeof(InterbedScreen));
        Assert.Equal(1, lookups.InterbedCalls);
        Assert.Equal(0, lookups.LineCalls);

        await main.NavigateAsync(typeof(AllGroupsScreen));
        Assert.Equal(2, lookups.InterbedCalls);
        Assert.Equal(1, lookups.LineCalls);
        Assert.Equal(100, main.Filters.SelectedInterbedId);
        Assert.Equal(10, main.Filters.SelectedLineId);
    }

    [Fact]
    public async Task Screens_without_filters_do_not_load_the_lists()
    {
        await main.NavigateAsync(typeof(TestScreen));
        await main.NavigateAsync(typeof(OtherScreen));

        Assert.Equal(0, lookups.InterbedCalls);
        Assert.Equal(0, lookups.LineCalls);
        Assert.Empty(lookups.BoreholeCalls);
    }
}
