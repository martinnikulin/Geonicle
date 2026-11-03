using Data.Models;
using Geonicle.Data.Services;
using Geonicle.Validation;
using Xunit;

namespace Geonicle.Data.Tests.Services;

/// <summary>Экран «Данные по скважинам»: выборка по скважине и прослою, справочники, проверка при сохранении.</summary>
public class HoleDataServiceTests
{
    private static (TestDb Db, int HoleA, int HoleB) Seed()
    {
        var testDb = new TestDb();
        using var db = testDb.CreateDbContext();
        var seam = new Seam { Description = "Пласт 1", ShortName = "П1" };
        var holeA = new Borehole { HoleName = "A" };
        var holeB = new Borehole { HoleName = "B" };
        db.AddRange(seam, holeA, holeB);
        db.SaveChanges();

        db.Lithologies.AddRange(
            new Lithology { BoreholeId = holeA.BoreholeId, DepthFrom = 0, DepthTo = 10 },
            new Lithology { BoreholeId = holeA.BoreholeId, DepthFrom = 10, DepthTo = 20 },
            new Lithology { BoreholeId = holeB.BoreholeId, DepthFrom = 0, DepthTo = 10 });
        db.SeamDefs.AddRange(
            new SeamDef { BoreholeId = holeA.BoreholeId, SeamId = seam.SeamId, InterbedId = 100, DepthFrom = 1, DepthTo = 2 },
            new SeamDef { BoreholeId = holeA.BoreholeId, SeamId = seam.SeamId, InterbedId = 200, DepthFrom = 3, DepthTo = 4 },
            new SeamDef { BoreholeId = holeB.BoreholeId, SeamId = seam.SeamId, InterbedId = 100, DepthFrom = 5, DepthTo = 6 });
        db.SaveChanges();
        return (testDb, holeA.BoreholeId, holeB.BoreholeId);
    }

    [Fact]
    public async Task Seams_are_limited_to_the_borehole_and_interbed()
    {
        var (testDb, holeA, _) = Seed();
        var service = new HoleDataService(testDb);

        var data = await service.LoadAsync(holeA, 100);

        var seam = Assert.Single(data.HoleSeams);
        Assert.Equal(holeA, seam.BoreholeId);
        Assert.Equal(100, seam.InterbedId);
    }

    [Fact]
    public async Task Lithology_is_the_whole_borehole_regardless_of_interbed()
    {
        var (testDb, holeA, holeB) = Seed();
        var service = new HoleDataService(testDb);

        Assert.Equal(2, (await service.LoadAsync(holeA, 100)).Lithology.Count);
        Assert.Equal(2, (await service.LoadAsync(holeA, 200)).Lithology.Count);
        Assert.Single((await service.LoadAsync(holeB, 100)).Lithology);
    }

    [Fact]
    public async Task Nothing_is_loaded_without_a_borehole_or_interbed()
    {
        var (testDb, holeA, _) = Seed();
        var service = new HoleDataService(testDb);

        var noHole = await service.LoadAsync(null, 100);
        Assert.Empty(noHole.HoleSeams);
        Assert.Empty(noHole.Lithology);

        var noInterbed = await service.LoadAsync(holeA, null);
        Assert.Empty(noInterbed.HoleSeams);
        Assert.Empty(noInterbed.Lithology);
    }

    [Fact]
    public async Task Reloading_replaces_the_previous_data()
    {
        var (testDb, holeA, holeB) = Seed();
        var service = new HoleDataService(testDb);

        await service.LoadAsync(holeA, 100);
        var data = await service.LoadAsync(holeB, 100);

        Assert.Single(data.Lithology);
        Assert.Equal(holeB, Assert.Single(data.HoleSeams).BoreholeId);
        Assert.False(service.HasChanges);
    }

    [Fact]
    public async Task Edited_layer_is_saved()
    {
        var (testDb, holeA, _) = Seed();
        var service = new HoleDataService(testDb);
        var data = await service.LoadAsync(holeA, 100);

        data.Lithology.First(l => l.DepthFrom == 0).Th = 7;
        Assert.True(service.HasChanges);
        await service.SaveAsync();

        using var db = testDb.CreateDbContext();
        Assert.Equal(7, db.Lithologies.Single(l => l.BoreholeId == holeA && l.DepthFrom == 0).Th);
    }

    [Fact]
    public async Task Duplicate_depth_from_blocks_the_save()
    {
        var (testDb, holeA, _) = Seed();
        var service = new HoleDataService(testDb);
        var data = await service.LoadAsync(holeA, 100);

        data.Lithology.Add(new Lithology { BoreholeId = holeA, DepthFrom = 10, DepthTo = 15 });

        var exception = await Assert.ThrowsAsync<DataValidationException>(() => service.SaveAsync());
        Assert.Contains(exception.Errors, e => e.EndsWith("Слой с такой глубиной «От» уже есть в этой скважине."));
    }

    [Fact]
    public async Task Invalid_draw_values_block_the_save()
    {
        var (testDb, holeA, _) = Seed();
        var service = new HoleDataService(testDb);
        var data = await service.LoadAsync(holeA, 100);

        var seam = data.HoleSeams.Single();
        seam.DrawFrom = 10;
        seam.DrawTo = 5;

        var exception = await Assert.ThrowsAsync<DataValidationException>(() => service.SaveAsync());
        Assert.Contains(exception.Errors, e => e.EndsWith("«Стопки до» не может быть меньше «Стопки от»."));
    }

    [Fact]
    public async Task Lookups_come_from_dictionary_sections_seams_and_rocks()
    {
        var testDb = new TestDb();
        using (var db = testDb.CreateDbContext())
        {
            db.Dictionaries.AddRange(
                new Dictionary { Section = "марка", N = 2, Description = "Ж" },
                new Dictionary { Section = "марка", N = 1, Description = "Г" },
                new Dictionary { Section = "тип угля", N = 1, Description = "Т" },
                new Dictionary { Section = "свита", N = 1, Description = "P2kz" },
                new Dictionary { Section = "другое", N = 1, Description = "не берём" });
            db.Seams.AddRange(new Seam { Description = "Б пласт", ShortName = "Б" }, new Seam { Description = "А пласт", ShortName = "А" });
            db.SaveChanges();
        }
        var service = new HoleDataService(testDb);
        await service.LoadAsync(null, null);

        Assert.Equal(new[] { "Г", "Ж" }, (await service.GetCoalGradeLookupAsync()).Select(i => i.Name));
        Assert.Equal(new[] { "Т" }, (await service.GetCoalTypeLookupAsync()).Select(i => i.Name));
        Assert.Equal(new[] { "P2kz" }, (await service.GetFormationLookupAsync()).Select(i => i.Name));
        // Породы — представление без ключа: в InMemory его не заполнить, проверяется только что запрос работает.
        Assert.Empty(await service.GetRockLookupAsync());
        Assert.Equal(new[] { "А пласт", "Б пласт" }, (await service.GetSeamLookupAsync()).Select(i => i.Name));
    }
}
