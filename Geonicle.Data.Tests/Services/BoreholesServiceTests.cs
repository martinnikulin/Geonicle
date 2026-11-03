using Data.Models;
using Geonicle.Data.Services;
using Geonicle.Validation;
using Xunit;

namespace Geonicle.Data.Tests.Services;

public class BoreholesServiceTests
{
    private static Borehole NewBorehole(string name = "A-1") =>
        new() { HoleName = name, Depth = 10, HoleType = HoleType.Скважина };

    [Fact]
    public async Task New_row_gets_a_zero_survey_immediately()
    {
        var service = new BoreholesService(new TestDb());
        var boreholes = await service.GetBoreholesAsync();

        var borehole = NewBorehole();
        boreholes.Add(borehole);   // как добавление строки в гриде, до сохранения

        var survey = Assert.Single(borehole.Surveys);
        Assert.Equal(0, survey.Depth);
        Assert.Equal(0, survey.Dip);
        Assert.Equal(0, survey.Azimuth);
        Assert.True(service.HasChanges);
    }

    [Fact]
    public async Task Manually_added_zero_survey_is_not_duplicated()
    {
        var service = new BoreholesService(new TestDb());
        var boreholes = await service.GetBoreholesAsync();

        var borehole = NewBorehole();
        borehole.Surveys.Add(new Survey { Depth = 0, Dip = 5, Azimuth = 7 });
        boreholes.Add(borehole);

        var survey = Assert.Single(borehole.Surveys);
        Assert.Equal(5, survey.Dip);
    }

    [Fact]
    public async Task Save_writes_borehole_and_all_surveys_and_nothing_else()
    {
        var testDb = new TestDb();
        var service = new BoreholesService(testDb);
        var boreholes = await service.GetBoreholesAsync();
        var borehole = NewBorehole();
        boreholes.Add(borehole);
        borehole.Surveys.Add(new Survey { Depth = 10, Dip = 1, Azimuth = 2 });
        borehole.Surveys.Add(new Survey { Depth = 20, Dip = 3, Azimuth = 4 });

        await service.SaveAsync();

        using var db = testDb.CreateDbContext();
        Assert.True(borehole.BoreholeId > 0);
        var depths = db.Surveys.Where(s => s.BoreholeId == borehole.BoreholeId).OrderBy(s => s.Depth).Select(s => s.Depth).ToList();
        Assert.Equal(new decimal[] { 0, 10, 20 }, depths);
        Assert.Empty(db.ChangedRecords);   // ChangedRecords и LineHoles остаются делом триггеров БД
        Assert.Empty(db.LineHoles);
        Assert.False(service.HasChanges);
    }

    [Fact]
    public async Task Loaded_borehole_keeps_its_surveys()
    {
        var testDb = new TestDb();
        var first = new BoreholesService(testDb);
        var boreholes = await first.GetBoreholesAsync();
        var borehole = NewBorehole();
        boreholes.Add(borehole);
        borehole.Surveys.Add(new Survey { Depth = 10 });
        await first.SaveAsync();

        var second = new BoreholesService(testDb);
        var loaded = await second.GetBoreholesAsync();

        Assert.Equal(2, Assert.Single(loaded).Surveys.Count);
        Assert.False(second.HasChanges);
    }

    [Fact]
    public async Task Invalid_data_is_not_saved_at_all_even_valid_rows()
    {
        var testDb = new TestDb();
        var service = new BoreholesService(testDb);
        var boreholes = await service.GetBoreholesAsync();
        boreholes.Add(NewBorehole("Good"));
        boreholes.Add(NewBorehole(""));

        var exception = await Assert.ThrowsAsync<DataValidationException>(() => service.SaveAsync());

        Assert.Contains("Скважина (без названия): Название скважины обязательно.", exception.Errors);
        using var db = testDb.CreateDbContext();
        Assert.Empty(db.Boreholes);
        Assert.True(service.HasChanges);   // правки остались, можно исправить и сохранить повторно
    }

    [Fact]
    public async Task After_fixing_the_errors_save_succeeds()
    {
        var testDb = new TestDb();
        var service = new BoreholesService(testDb);
        var boreholes = await service.GetBoreholesAsync();
        var bad = NewBorehole("");
        boreholes.Add(bad);
        await Assert.ThrowsAsync<DataValidationException>(() => service.SaveAsync());

        bad.HoleName = "Fixed";
        await service.SaveAsync();

        using var db = testDb.CreateDbContext();
        Assert.Equal("Fixed", Assert.Single(db.Boreholes).HoleName);
    }

    [Fact]
    public async Task Removing_a_row_deletes_it_with_its_surveys()
    {
        var testDb = new TestDb();
        var service = new BoreholesService(testDb);
        var boreholes = await service.GetBoreholesAsync();
        var borehole = NewBorehole();
        boreholes.Add(borehole);
        await service.SaveAsync();

        boreholes.Remove(borehole);
        await service.SaveAsync();

        using var db = testDb.CreateDbContext();
        Assert.Empty(db.Boreholes);
        Assert.Empty(db.Surveys);
    }
}
