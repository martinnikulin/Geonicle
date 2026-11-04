using Data.Models;
using Geonicle.Infrastructure;
using Xunit;

namespace Geonicle.Data.Tests.Services;

public class FilterLookupServiceTests
{
    private readonly TestDb testDb = new();

    private FilterLookupService Service() => new(testDb);

    private static CondDef Cond(string section, int n, string name, bool actual = true) =>
        new() { Section = section, N = n, CondName = name, Actual = actual };

    [Fact]
    public async Task Interbeds_are_the_actual_conditions_of_the_Interbed_section_with_key_N_times_100()
    {
        using (var db = testDb.CreateDbContext())
        {
            db.CondDefs.AddRange(
                Cond("Прослой", 2, "0,5 м"),
                Cond("Прослой", 1, "0,3 м"),
                Cond("Прослой", 3, "устарел", actual: false),
                Cond("Другой раздел", 4, "чужой"));
            db.SaveChanges();
        }

        var items = await Service().GetInterbedsAsync();

        Assert.Equal(
            new[] { (100, "Прослой 0,3 м"), (200, "Прослой 0,5 м") },
            items.Select(i => (i.InterbedId, i.InterbedName)));
    }

    [Fact]
    public async Task Seams_are_the_actual_ones_ordered_by_N_with_the_description()
    {
        using (var db = testDb.CreateDbContext())
        {
            db.Seams.AddRange(
                new Seam { Description = "Пласт 2", ShortName = "П2", N = 2, Actual = true },
                new Seam { Description = "Пласт 1", ShortName = "П1", N = 1, Actual = true },
                new Seam { Description = "Устарел", ShortName = "У", N = 0, Actual = false },
                new Seam { Description = null, ShortName = "Без", N = 3, Actual = true });
            db.SaveChanges();
        }

        var items = await Service().GetSeamsAsync();

        Assert.Equal(new[] { "Пласт 1", "Пласт 2", "" }, items.Select(s => s.Description));
    }

    [Fact]
    public async Task Lines_are_ordered_by_N_and_carry_a_display_text()
    {
        using (var db = testDb.CreateDbContext())
        {
            db.Lines.AddRange(
                new Line { Description = "Вторая", N = 2, LineType = LineType.Канава },
                new Line { Description = "Первая", N = 1, LineType = LineType.Канава });
            db.SaveChanges();
        }

        var lines = await Service().GetLinesAsync();

        Assert.Equal(new[] { "Первая", "Вторая" }, lines.Select(l => l.Description));
        Assert.Equal("1 — Первая", lines[0].DisplayName);
    }

    /// <summary>Две линии и три скважины: A в обеих линиях, B только в первой, C только во второй.</summary>
    private (int line1, int line2) SeedLineHoles()
    {
        using var db = testDb.CreateDbContext();
        var a = new Borehole { HoleName = "A", HoleType = HoleType.Скважина };
        var b = new Borehole { HoleName = "B", HoleType = HoleType.Скважина };
        var c = new Borehole { HoleName = "C", HoleType = HoleType.Скважина };
        var line1 = new Line { Description = "Л1", N = 1, LineType = LineType.Канава };
        var line2 = new Line { Description = "Л2", N = 2, LineType = LineType.Канава };
        db.AddRange(a, b, c, line1, line2);
        db.SaveChanges();
        db.LineHoles.AddRange(
            new LineHole { LineId = line1.LineId, BoreholeId = b.BoreholeId, N = 2 },
            new LineHole { LineId = line1.LineId, BoreholeId = a.BoreholeId, N = 1 },
            new LineHole { LineId = line2.LineId, BoreholeId = a.BoreholeId, N = 5 },
            new LineHole { LineId = line2.LineId, BoreholeId = c.BoreholeId, N = 6 });
        db.SaveChanges();
        return (line1.LineId, line2.LineId);
    }

    [Fact]
    public async Task Boreholes_of_a_line_are_ordered_by_N_and_include_N()
    {
        var (line1, _) = SeedLineHoles();

        var items = await Service().GetLineBoreholesAsync(line1, allBoreholes: false);

        Assert.Equal(new[] { ("A", 1m), ("B", 2m) }, items.Select(i => (i.HoleName, i.N.Value)));
    }

    [Fact]
    public async Task Another_line_gives_its_own_boreholes()
    {
        var (_, line2) = SeedLineHoles();

        var items = await Service().GetLineBoreholesAsync(line2, allBoreholes: false);

        Assert.Equal(new[] { "A", "C" }, items.Select(i => i.HoleName));
    }

    [Fact]
    public async Task All_boreholes_ignores_the_line_and_has_no_repeats()
    {
        var (line1, _) = SeedLineHoles();

        var items = await Service().GetLineBoreholesAsync(line1, allBoreholes: true);

        Assert.Equal(new[] { "A", "B", "C" }, items.Select(i => i.HoleName));   // A есть в обеих линиях, но один раз
        Assert.All(items, i => Assert.Null(i.N));
    }

    [Fact]
    public async Task Without_a_line_and_without_all_the_list_is_empty()
    {
        SeedLineHoles();

        Assert.Empty(await Service().GetLineBoreholesAsync(null, allBoreholes: false));
    }

    [Fact]
    public async Task Boreholes_without_line_holes_are_not_listed_even_in_all_mode()
    {
        using (var db = testDb.CreateDbContext())
        {
            db.Boreholes.Add(new Borehole { HoleName = "Одинокая", HoleType = HoleType.Скважина });
            db.SaveChanges();
        }

        Assert.Empty(await Service().GetLineBoreholesAsync(null, allBoreholes: true));
    }
}
