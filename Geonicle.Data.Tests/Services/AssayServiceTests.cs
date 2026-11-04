using Data.Models;
using Geonicle.Data.Services;
using Geonicle.Validation;
using Xunit;

namespace Geonicle.Data.Tests.Services;

/// <summary>Форма «Анализы» пробы: строки по показателям и запись изменений (сравнение, а не «удалить всё и вставить»).</summary>
public class AssayServiceTests
{
    private const int Sample = 7;

    private static AssayVarInfo Var(int id, double n, string name, int group, string groupName, int groupN) =>
        new(id, n, name, group, groupName, groupN);

    private static IReadOnlyList<AssayRow> Rows(params (int VarId, string? Val)[] values) =>
        values.Select(v => new AssayRow(v.VarId, v.VarId, $"П{v.VarId}", 1, "Группа", 1, v.Val)).ToList();

    private static void Seed(TestDb testDb, params (int SampleId, int VarId, string? Val)[] assays)
    {
        using var db = testDb.CreateDbContext();
        db.Assays.AddRange(assays.Select(a => new Assay { SampleId = a.SampleId, VarId = a.VarId, Val = a.Val }));
        db.SaveChanges();
    }

    private static Dictionary<int, string?> Saved(TestDb testDb, int sampleId)
    {
        using var db = testDb.CreateDbContext();
        return db.Assays.Where(a => a.SampleId == sampleId).ToDictionary(a => a.VarId, a => a.Val);
    }

    [Fact]
    public void Rows_keep_the_order_and_take_values_by_variable_with_empty_where_there_is_none()
    {
        var vars = new[]
        {
            Var(2, 2, "Ad флот., %", 1, "Технический анализ", 1),
            Var(1, 1, "Ad, %", 1, "Технический анализ", 1),
            Var(9, 1, "Cl, %", 2, "Элементный состав", 2),
        };

        var rows = AssayService.BuildRows(vars, new Dictionary<int, string?> { [1] = "28,15", [5] = "не в списке" });

        Assert.Equal(new[] { 2, 1, 9 }, rows.Select(r => r.VarId));
        Assert.Equal(new string?[] { null, "28,15", null }, rows.Select(r => r.Val));
        Assert.Equal("Элементный состав", rows[2].GroupName);
        Assert.Equal(2, rows[2].GroupN);
    }

    [Fact]
    public async Task Non_empty_values_are_added_and_empty_ones_are_not()
    {
        var testDb = new TestDb();
        var service = new AssayService(testDb);

        await service.SaveAsync(Sample, Rows((1, "28,15"), (2, null), (3, "  "), (4, "")));

        Assert.Equal(new Dictionary<int, string?> { [1] = "28,15" }, Saved(testDb, Sample));
    }

    [Fact]
    public async Task Changed_values_are_updated_and_cleared_values_are_deleted()
    {
        var testDb = new TestDb();
        Seed(testDb, (Sample, 1, "10"), (Sample, 2, "20"), (Sample, 3, "30"));
        var service = new AssayService(testDb);

        await service.SaveAsync(Sample, Rows((1, "11"), (2, ""), (3, "30")));

        Assert.Equal(new Dictionary<int, string?> { [1] = "11", [3] = "30" }, Saved(testDb, Sample));
    }

    [Fact]
    public async Task Unchanged_rows_are_not_rewritten()
    {
        var testDb = new TestDb();
        Seed(testDb, (Sample, 1, "10"), (Sample, 2, "20"));
        var created = DateTime.Today.AddYears(-5);
        using (var db = testDb.CreateDbContext())
        {
            foreach (var assay in db.Assays)
                assay.DateCreated = created;
            db.SaveChanges();
        }
        var service = new AssayService(testDb);

        await service.SaveAsync(Sample, Rows((1, "10"), (2, "21")));

        using var read = testDb.CreateDbContext();
        Assert.Equal(created, read.Assays.Single(a => a.VarId == 1).DateCreated);   // не пересоздавалась
        Assert.Equal(created, read.Assays.Single(a => a.VarId == 2).DateCreated);   // обновлена, а не вставлена заново
        Assert.Equal("21", read.Assays.Single(a => a.VarId == 2).Val);
    }

    [Fact]
    public async Task Values_of_variables_outside_the_form_and_of_other_samples_are_not_touched()
    {
        var testDb = new TestDb();
        Seed(testDb, (Sample, 1, "10"), (Sample, 99, "неактуальный показатель"), (8, 1, "чужая проба"));
        var service = new AssayService(testDb);

        await service.SaveAsync(Sample, Rows((1, "")));   // форма знает только показатель 1

        Assert.Equal(new Dictionary<int, string?> { [99] = "неактуальный показатель" }, Saved(testDb, Sample));
        Assert.Equal(new Dictionary<int, string?> { [1] = "чужая проба" }, Saved(testDb, 8));
    }

    [Fact]
    public async Task Values_are_trimmed_when_saved()
    {
        var testDb = new TestDb();
        var service = new AssayService(testDb);

        await service.SaveAsync(Sample, Rows((1, "  28,15 ")));

        Assert.Equal("28,15", Saved(testDb, Sample)[1]);
    }

    [Fact]
    public async Task Too_long_value_blocks_the_whole_save()
    {
        var testDb = new TestDb();
        Seed(testDb, (Sample, 1, "10"));
        var service = new AssayService(testDb);

        var error = await Assert.ThrowsAsync<DataValidationException>(
            () => service.SaveAsync(Sample, Rows((1, "11"), (2, new string('9', 201)))));

        Assert.Contains(error.Errors, e => e.EndsWith("не может быть длиннее 200 символов."));
        Assert.Equal(new Dictionary<int, string?> { [1] = "10" }, Saved(testDb, Sample));   // ничего не записано
    }

    [Fact]
    public async Task Loading_without_reference_data_gives_no_rows()
    {
        // Показатели и группы — представления без ключа: в InMemory их не заполнить, проверяем только что запрос выполняется.
        var service = new AssayService(new TestDb());
        Assert.Empty(await service.LoadAsync(Sample));
    }
}
