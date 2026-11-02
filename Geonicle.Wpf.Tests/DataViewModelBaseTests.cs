using System.Windows;
using System.Windows.Input;
using Xunit;

namespace Geonicle.Wpf.Tests;

public class DataViewModelBaseTests
{
    private readonly FakeDialogs dialogs = new();

    private TestScreen Screen() => new(dialogs);

    [Fact]
    public async Task Load_marks_the_screen_initialized_and_disables_the_initialize_command()
    {
        var screen = Screen();
        Assert.True(((ICommand)screen.InitializeCommand).CanExecute(null));

        await screen.LoadAsync();

        Assert.True(screen.IsInitialized);
        Assert.False(((ICommand)screen.InitializeCommand).CanExecute(null));
        Assert.Equal(1, screen.Loads);
    }

    [Fact]
    public async Task HasChanges_is_false_until_loaded_and_follows_pending_changes_afterwards()
    {
        var screen = Screen();
        screen.Pending = true;
        Assert.False(screen.HasChanges);

        await screen.LoadAsync();

        Assert.True(screen.HasChanges);
        screen.Pending = false;
        Assert.False(screen.HasChanges);
    }

    [Fact]
    public async Task Successful_save_raises_Saved_with_the_screen_as_sender()
    {
        var screen = Screen();
        object sender = null;
        var raised = 0;
        screen.Saved += (s, _) => { sender = s; raised++; };

        await screen.SaveAsync();

        Assert.Equal(1, raised);
        Assert.Same(screen, sender);
    }

    [Fact]
    public async Task Failed_save_does_not_raise_Saved_and_the_error_propagates()
    {
        var screen = Screen();
        screen.SaveError = new InvalidOperationException("нет связи");
        var raised = 0;
        screen.Saved += (_, _) => raised++;

        await Assert.ThrowsAsync<InvalidOperationException>(() => screen.SaveAsync());

        Assert.Equal(0, raised);
    }

    [Fact]
    public async Task Refresh_without_changes_reloads_without_asking()
    {
        var screen = Screen();
        await screen.LoadAsync();

        await screen.RefreshAsync();

        Assert.Equal(2, screen.Loads);
        Assert.Empty(dialogs.Shown);
    }

    [Fact]
    public async Task Refresh_with_changes_asks_and_does_nothing_when_the_user_says_no()
    {
        var screen = Screen();
        await screen.LoadAsync();
        screen.Pending = true;
        dialogs.Answer(MessageBoxResult.No);

        await screen.RefreshAsync();

        Assert.Equal(1, screen.Loads);
        var asked = Assert.Single(dialogs.Shown);
        Assert.Equal("Несохранённые изменения будут потеряны. Обновить данные?", asked.Text);
        Assert.Equal(MessageBoxButton.YesNo, asked.Buttons);
    }

    [Fact]
    public async Task Refresh_with_changes_reloads_when_the_user_says_yes()
    {
        var screen = Screen();
        await screen.LoadAsync();
        screen.Pending = true;
        dialogs.Answer(MessageBoxResult.Yes);

        await screen.RefreshAsync();

        Assert.Equal(2, screen.Loads);
    }

    [Fact]
    public async Task Refresh_shows_a_load_error_instead_of_throwing()
    {
        var screen = Screen();
        await screen.LoadAsync();
        screen.LoadError = new InvalidOperationException("сервер недоступен");

        await screen.RefreshAsync();

        var shown = Assert.Single(dialogs.Shown);
        Assert.Equal("Ошибка загрузки", shown.Caption);
        Assert.Equal("сервер недоступен", shown.Text);
        Assert.Equal(MessageBoxImage.Error, shown.Icon);
    }

    [Fact]
    public void Grid_menu_starts_with_Refresh_bound_to_the_refresh_command()
    {
        var screen = Screen();

        var item = Assert.Single(screen.Menu.Items);

        Assert.Equal("Обновить", item.Caption);
        Assert.Same(screen.RefreshCommand, item.Command);
    }
}
