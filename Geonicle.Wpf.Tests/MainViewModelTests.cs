using System.Windows;
using Geonicle.Validation;
using Geonicle.Wpf.Services;
using Geonicle.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Geonicle.Wpf.Tests;

public class MainViewModelTests
{
    private readonly FakeDialogs dialogs = new();
    private readonly MainViewModel main;

    public MainViewModelTests()
    {
        var provider = new ServiceCollection()
            .AddSingleton<IUserDialogs>(dialogs)
            .AddTransient<TestScreen>()
            .AddTransient<OtherScreen>()
            .BuildServiceProvider();
        main = new MainViewModel(provider, dialogs);
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
}
