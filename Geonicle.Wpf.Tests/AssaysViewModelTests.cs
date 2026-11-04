using System.Windows;
using DevExpress.Data.Filtering;
using Geonicle.Data.Services;
using Geonicle.Validation;
using Geonicle.Wpf.ViewModels;
using Xunit;

namespace Geonicle.Wpf.Tests;

/// <summary>Заглушка сервиса анализов: строки для пробы и запоминание записанного.</summary>
public sealed class FakeAssayService : IAssayService
{
    public List<AssayRow> Rows { get; } = new();

    public Exception LoadError { get; set; }

    public Exception SaveError { get; set; }

    public List<(int SampleId, IReadOnlyList<AssayRow> Rows)> SavedCalls { get; } = new();

    public Task<IReadOnlyList<AssayRow>> LoadAsync(int sampleId)
    {
        if (LoadError != null) throw LoadError;
        return Task.FromResult<IReadOnlyList<AssayRow>>(Rows.ToList());
    }

    public Task SaveAsync(int sampleId, IReadOnlyList<AssayRow> rows)
    {
        if (SaveError != null) throw SaveError;
        SavedCalls.Add((sampleId, rows.ToList()));
        return Task.CompletedTask;
    }
}

public class AssaysViewModelTests
{
    private readonly FakeDialogs dialogs = new();
    private readonly FakeAssayService service = new();

    public AssaysViewModelTests()
    {
        service.Rows.Add(new AssayRow(1, 1, "Ad, %", 1, "Технический анализ", 1, "28,15"));
        service.Rows.Add(new AssayRow(2, 2, "Ad флот., %", 1, "Технический анализ", 1, null));
    }

    private void AddSecondGroup()
    {
        // Группа «Элементный состав» идёт после «Технического анализа» по порядку групп, хотя её строка в списке раньше.
        service.Rows.Insert(0, new AssayRow(9, 1, "Cl, %", 2, "Элементный состав", 2, "0,1"));
    }

    /// <summary>Условие грида как (поле, значение): так тесты не зависят от текстового вида условия.</summary>
    private static (string Field, int Value) GroupCondition(CriteriaOperator criteria)
    {
        var binary = Assert.IsType<BinaryOperator>(criteria);
        Assert.Equal(BinaryOperatorType.Equal, binary.OperatorType);
        return (
            Assert.IsType<OperandProperty>(binary.LeftOperand).PropertyName,
            Convert.ToInt32(Assert.IsType<OperandValue>(binary.RightOperand).Value));
    }

    private AssaysViewModel Window() => new(service, dialogs);

    [Fact]
    public async Task Initialize_loads_the_rows_and_sets_the_title()
    {
        var window = Window();

        await window.InitializeAsync(42, "Анализы: проба № 1");

        Assert.Equal("Анализы: проба № 1", window.Title);
        Assert.Equal(new[] { 1, 2 }, window.Rows.Select(r => r.VarId));
    }

    [Fact]
    public async Task Group_tabs_follow_the_group_order_and_the_first_group_is_selected()
    {
        AddSecondGroup();
        var window = Window();

        await window.InitializeAsync(42, "t");

        Assert.Equal(new[] { "Технический анализ", "Элементный состав" }, window.Groups.Select(g => g.GroupName));
        Assert.Equal(1, window.SelectedGroup.GroupId);
        Assert.Equal(("GroupId", 1), GroupCondition(window.FilterCriteria));
    }

    [Fact]
    public async Task Grouping_is_on_by_default_like_in_the_original_form()
    {
        var window = Window();
        await window.InitializeAsync(42, "t");

        Assert.True(window.GroupByGroups);
        Assert.NotNull(window.FilterCriteria);
    }

    [Fact]
    public async Task Selecting_a_tab_filters_the_grid_by_that_group()
    {
        AddSecondGroup();
        var window = Window();
        await window.InitializeAsync(42, "t");
        var changed = new List<string>();
        window.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        window.SelectedGroup = window.Groups[1];

        Assert.Equal(("GroupId", 2), GroupCondition(window.FilterCriteria));
        Assert.Contains(nameof(AssaysViewModel.FilterCriteria), changed);
    }

    [Fact]
    public async Task Switching_the_by_groups_flag_off_shows_all_indicators_and_on_restores_the_group_filter()
    {
        AddSecondGroup();
        var window = Window();
        await window.InitializeAsync(42, "t");
        window.SelectedGroup = window.Groups[1];

        window.GroupByGroups = false;
        Assert.Null(window.FilterCriteria);

        window.GroupByGroups = true;
        Assert.Equal(("GroupId", 2), GroupCondition(window.FilterCriteria));   // выбранный таб сохранился
    }

    [Fact]
    public async Task Edits_survive_switching_tabs_and_ok_saves_every_group()
    {
        AddSecondGroup();
        var window = Window();
        await window.InitializeAsync(42, "t");
        window.Rows.Single(r => r.VarId == 1).Val = "30";
        window.SelectedGroup = window.Groups[1];
        window.Rows.Single(r => r.VarId == 9).Val = "0,2";
        window.SelectedGroup = window.Groups[0];

        await window.OkAsync();

        var saved = Assert.Single(service.SavedCalls).Rows;
        Assert.Equal("30", saved.Single(r => r.VarId == 1).Val);
        Assert.Equal("0,2", saved.Single(r => r.VarId == 9).Val);
    }

    [Fact]
    public async Task Without_indicators_there_are_no_tabs_and_no_filter()
    {
        service.Rows.Clear();
        var window = Window();

        await window.InitializeAsync(42, "t");

        Assert.Empty(window.Groups);
        Assert.Null(window.SelectedGroup);
        Assert.Null(window.FilterCriteria);
    }

    [Fact]
    public async Task Ok_saves_the_edited_rows_for_the_sample_then_reports_saved_and_closes()
    {
        var window = Window();
        await window.InitializeAsync(42, "t");
        var events = new List<string>();
        window.Saved += (_, _) => events.Add("saved");
        window.CloseRequested += (_, _) => events.Add("close");
        window.Rows[1].Val = "14,45";

        await window.OkAsync();

        var call = Assert.Single(service.SavedCalls);
        Assert.Equal(42, call.SampleId);
        Assert.Equal(new string[] { "28,15", "14,45" }, call.Rows.Select(r => r.Val));
        Assert.Equal(new[] { "saved", "close" }, events);
    }

    [Fact]
    public async Task Cancel_closes_without_saving()
    {
        var window = Window();
        await window.InitializeAsync(42, "t");
        var closed = 0;
        window.CloseRequested += (_, _) => closed++;
        window.Rows[0].Val = "99";

        window.Cancel();

        Assert.Equal(1, closed);
        Assert.Empty(service.SavedCalls);
    }

    [Fact]
    public async Task Validation_errors_keep_the_window_open_and_are_shown()
    {
        var window = Window();
        await window.InitializeAsync(42, "t");
        service.SaveError = new DataValidationException(new[] { "Анализ показателя 2: слишком длинное значение" });
        var closed = 0;
        window.CloseRequested += (_, _) => closed++;

        await window.OkAsync();

        Assert.Equal(0, closed);
        var shown = Assert.Single(dialogs.Shown);
        Assert.Contains("слишком длинное значение", shown.Text);
        Assert.Equal(MessageBoxImage.Warning, shown.Icon);
    }

    [Fact]
    public async Task Other_save_errors_keep_the_window_open_and_are_shown()
    {
        var window = Window();
        await window.InitializeAsync(42, "t");
        service.SaveError = new InvalidOperationException("нет связи");
        var saved = 0;
        window.Saved += (_, _) => saved++;

        await window.OkAsync();

        Assert.Equal(0, saved);
        Assert.Equal("нет связи", Assert.Single(dialogs.Shown).Text);
        Assert.Equal(MessageBoxImage.Error, dialogs.Shown[0].Icon);
    }

    [Fact]
    public async Task Load_error_is_shown_and_ok_does_nothing()
    {
        service.LoadError = new InvalidOperationException("БД недоступна");
        var window = Window();

        await window.InitializeAsync(42, "t");
        await window.OkAsync();

        Assert.Equal("БД недоступна", Assert.Single(dialogs.Shown).Text);
        Assert.Empty(service.SavedCalls);
    }
}
