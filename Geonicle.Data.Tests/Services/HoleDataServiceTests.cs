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

        var a1 = new Lithology { BoreholeId = holeA.BoreholeId, DepthFrom = 0, DepthTo = 10 };
        var a2 = new Lithology { BoreholeId = holeA.BoreholeId, DepthFrom = 10, DepthTo = 20 };
        var a3 = new Lithology { BoreholeId = holeA.BoreholeId, DepthFrom = 20, DepthTo = 30 };
        var b1 = new Lithology { BoreholeId = holeB.BoreholeId, DepthFrom = 0, DepthTo = 10 };
        db.Lithologies.AddRange(a1, a2, a3, b1);
        db.SaveChanges();

        // Глубины пластопересечения берутся из слоёв-границ: «От» — слой DepthFromId, «До» — слой DepthToId.
        db.SeamDefs.AddRange(
            new SeamDef { BoreholeId = holeA.BoreholeId, SeamId = seam.SeamId, InterbedId = 100, DepthFromId = a2.LithologyId, DepthToId = a3.LithologyId },
            new SeamDef { BoreholeId = holeA.BoreholeId, SeamId = seam.SeamId, InterbedId = 200, DepthFromId = a1.LithologyId, DepthToId = a1.LithologyId },
            new SeamDef { BoreholeId = holeB.BoreholeId, SeamId = seam.SeamId, InterbedId = 100, DepthFromId = b1.LithologyId, DepthToId = b1.LithologyId });
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
    public async Task Seam_depths_come_from_the_boundary_lithology_layers()
    {
        var (testDb, holeA, _) = Seed();
        var service = new HoleDataService(testDb);

        var seam = Assert.Single((await service.LoadAsync(holeA, 100)).HoleSeams);

        Assert.Equal(10, seam.DepthFrom);   // DepthFrom слоя DepthFromId
        Assert.Equal(30, seam.DepthTo);     // DepthTo слоя DepthToId
    }

    [Fact]
    public async Task Layers_get_the_name_of_the_seam_that_contains_them()
    {
        var (testDb, holeA, _) = Seed();
        var service = new HoleDataService(testDb);

        var data = await service.LoadAsync(holeA, 100);

        // Пластопересечение прослоя 100 — от 10 до 30: слои 10–20 и 20–30 входят в него, слой 0–10 нет.
        Assert.Null(data.Lithology.Single(l => l.DepthFrom == 0).SeamName);
        Assert.Equal("Пласт 1", data.Lithology.Single(l => l.DepthFrom == 10).SeamName);
        Assert.Equal("Пласт 1", data.Lithology.Single(l => l.DepthFrom == 20).SeamName);
    }

    [Fact]
    public async Task Seam_names_follow_edited_depths_after_UpdateSeamNames()
    {
        var (testDb, holeA, _) = Seed();
        var service = new HoleDataService(testDb);
        var data = await service.LoadAsync(holeA, 100);
        var layer = data.Lithology.Single(l => l.DepthFrom == 0);
        Assert.Null(layer.SeamName);

        layer.DepthFrom = 10;   // слой теперь внутри 10–30 (глубины пласта не меняются)
        layer.DepthTo = 15;
        service.UpdateSeamNames();

        Assert.Equal("Пласт 1", layer.SeamName);
    }

    [Fact]
    public async Task Seams_are_ordered_by_depth()
    {
        var testDb = new TestDb();
        using (var db = testDb.CreateDbContext())
        {
            var seam = new Seam { Description = "П", ShortName = "П" };
            var hole = new Borehole { HoleName = "A" };
            db.AddRange(seam, hole);
            db.SaveChanges();
            var upper = new Lithology { BoreholeId = hole.BoreholeId, DepthFrom = 0, DepthTo = 5 };
            var lower = new Lithology { BoreholeId = hole.BoreholeId, DepthFrom = 50, DepthTo = 60 };
            db.Lithologies.AddRange(upper, lower);
            db.SaveChanges();
            db.SeamDefs.AddRange(   // нижний пласт добавлен первым
                new SeamDef { BoreholeId = hole.BoreholeId, SeamId = seam.SeamId, InterbedId = 100, DepthFromId = lower.LithologyId, DepthToId = lower.LithologyId },
                new SeamDef { BoreholeId = hole.BoreholeId, SeamId = seam.SeamId, InterbedId = 100, DepthFromId = upper.LithologyId, DepthToId = upper.LithologyId });
            db.SaveChanges();
        }
        var service = new HoleDataService(testDb);
        int holeId;
        using (var db = testDb.CreateDbContext())
            holeId = db.Boreholes.Single().BoreholeId;

        var seams = (await service.LoadAsync(holeId, 100)).HoleSeams;

        Assert.Equal(new decimal?[] { 0, 50 }, seams.Select(s => s.DepthFrom));
    }

    [Fact]
    public async Task Lithology_is_the_whole_borehole_regardless_of_interbed()
    {
        var (testDb, holeA, holeB) = Seed();
        var service = new HoleDataService(testDb);

        Assert.Equal(3, (await service.LoadAsync(holeA, 100)).Lithology.Count);
        Assert.Equal(3, (await service.LoadAsync(holeA, 200)).Lithology.Count);
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
    public async Task Layer_added_to_the_list_gets_the_borehole_and_is_saved()
    {
        var (testDb, holeA, _) = Seed();
        var service = new HoleDataService(testDb);
        var data = await service.LoadAsync(holeA, 100);

        data.Lithology.Add(new Lithology { DepthFrom = 40, DepthTo = 50 });   // BoreholeId не задан: подставляется коллекцией скважины
        await service.SaveAsync();

        using var db = testDb.CreateDbContext();
        Assert.Equal(holeA, db.Lithologies.Single(l => l.DepthFrom == 40).BoreholeId);
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

/// <summary>Добавление и переопределение пластопересечений по выбранным строкам литологии.</summary>
public class HoleSeamEditingTests
{
    private sealed record Setup(TestDb Db, int Hole, int NoNameSeamId);

    private static Setup Seed()
    {
        var testDb = new TestDb();
        using var db = testDb.CreateDbContext();
        // В InMemory ключ не может быть 0, поэтому «пласт без названия» получает свой код, который сервису сообщается явно.
        var noName = new Seam { Description = "", ShortName = "" };
        var named = new Seam { Description = "Пласт 1", ShortName = "П1" };
        var hole = new Borehole { HoleName = "A" };
        db.AddRange(noName, named, hole);
        db.SaveChanges();
        db.Lithologies.AddRange(
            new Lithology { BoreholeId = hole.BoreholeId, DepthFrom = 0, DepthTo = 10 },
            new Lithology { BoreholeId = hole.BoreholeId, DepthFrom = 10, DepthTo = 20 },
            new Lithology { BoreholeId = hole.BoreholeId, DepthFrom = 20, DepthTo = 30 },
            new Lithology { BoreholeId = hole.BoreholeId, DepthFrom = 30, DepthTo = 40 });
        db.SaveChanges();
        return new Setup(testDb, hole.BoreholeId, noName.SeamId);
    }

    private static HoleDataService Service(Setup s) => new(s.Db) { NoNameSeamId = s.NoNameSeamId };

    private static IReadOnlyList<Lithology> Layers(HoleData data, params decimal[] depthFrom) =>
        depthFrom.Select(d => data.Lithology.Single(l => l.DepthFrom == d)).ToList();

    [Fact]
    public async Task Adding_a_seam_uses_the_first_and_last_selected_layers_and_the_no_name_seam()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(setup.Hole, 100);

        var seam = await service.AddHoleSeamAsync(Layers(data, 10, 20, 30));

        Assert.Equal(setup.NoNameSeamId, seam.SeamId);
        Assert.Equal(setup.Hole, seam.BoreholeId);
        Assert.Equal(100, seam.InterbedId);
        Assert.Equal(10, seam.DepthFrom);   // верхняя граница — первая выбранная строка
        Assert.Equal(40, seam.DepthTo);     // нижняя — последняя
        Assert.Equal(10, seam.DrawFrom);    // стопки по умолчанию как глубины границ
        Assert.Equal(40, seam.DrawTo);
        Assert.Contains(seam, data.HoleSeams);   // виден в гриде сразу, до сохранения
    }

    [Fact]
    public async Task Added_seam_is_saved_and_layers_inside_get_its_name()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(setup.Hole, 100);

        await service.AddHoleSeamAsync(Layers(data, 10, 20));
        await service.SaveAsync();

        using var db = testDbContext(setup);
        var saved = db.SeamDefs.Single();
        Assert.Equal(setup.NoNameSeamId, saved.SeamId);
        Assert.Equal(setup.Hole, saved.BoreholeId);
        Assert.True(saved.HoleSeamId > 0);
        Assert.Equal(db.Lithologies.Single(l => l.DepthFrom == 10).LithologyId, saved.DepthFromId);
        Assert.Equal(db.Lithologies.Single(l => l.DepthFrom == 20).LithologyId, saved.DepthToId);
    }

    [Fact]
    public async Task A_single_selected_layer_makes_a_seam_of_that_layer()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(setup.Hole, 100);

        var seam = await service.AddHoleSeamAsync(Layers(data, 20));

        Assert.Equal(20, seam.DepthFrom);
        Assert.Equal(30, seam.DepthTo);
    }

    [Fact]
    public async Task Layers_inside_a_new_seam_get_the_seam_name_column_recalculated()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(setup.Hole, 100);
        Assert.All(data.Lithology, l => Assert.Null(l.SeamName));

        await service.AddHoleSeamAsync(Layers(data, 10, 20));

        // Название пласта без названия пустое, но слои вне пластопересечения остаются null, внутри получают ""
        Assert.Null(data.Lithology.Single(l => l.DepthFrom == 0).SeamName);
        Assert.Equal("", data.Lithology.Single(l => l.DepthFrom == 10).SeamName);
        Assert.Equal("", data.Lithology.Single(l => l.DepthFrom == 20).SeamName);
        Assert.Null(data.Lithology.Single(l => l.DepthFrom == 30).SeamName);
    }

    [Fact]
    public async Task Adding_without_selected_layers_is_rejected()
    {
        var setup = Seed();
        var service = Service(setup);
        await service.LoadAsync(setup.Hole, 100);

        var error = await Assert.ThrowsAsync<HoleDataException>(() => service.AddHoleSeamAsync(new List<Lithology>()));
        Assert.Contains("Выберите строки литологии", error.Message);
    }

    [Fact]
    public async Task Unsaved_layers_in_the_selection_are_rejected()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(setup.Hole, 100);
        var unsaved = new Lithology { DepthFrom = 40, DepthTo = 50 };
        data.Lithology.Add(unsaved);

        var error = await Assert.ThrowsAsync<HoleDataException>(() => service.AddHoleSeamAsync(new[] { Layers(data, 30)[0], unsaved }));
        Assert.Contains("несохранённые", error.Message);
    }

    [Fact]
    public async Task Missing_no_name_seam_gives_a_clear_error()
    {
        var setup = Seed();
        var service = new HoleDataService(setup.Db) { NoNameSeamId = 12345 };
        var data = await service.LoadAsync(setup.Hole, 100);

        var error = await Assert.ThrowsAsync<HoleDataException>(() => service.AddHoleSeamAsync(Layers(data, 10)));
        Assert.Contains("нет «пласта без названия»", error.Message);
    }

    [Fact]
    public async Task Adding_without_a_loaded_borehole_is_rejected()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(null, 100);

        await Assert.ThrowsAsync<HoleDataException>(() => service.AddHoleSeamAsync(new[] { new Lithology { LithologyId = 1 } }));
    }

    [Fact]
    public async Task Redefining_changes_only_the_boundaries()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(setup.Hole, 100);
        var seam = await service.AddHoleSeamAsync(Layers(data, 0, 10));
        seam.Comments = "оставить";
        await service.SaveAsync();

        await service.RedefineHoleSeamAsync(seam, Layers(data, 20, 30));
        await service.SaveAsync();

        Assert.Equal(20, seam.DepthFrom);
        Assert.Equal(40, seam.DepthTo);
        using var db = testDbContext(setup);
        var saved = db.SeamDefs.Single();
        Assert.Equal("оставить", saved.Comments);
        Assert.Equal(db.Lithologies.Single(l => l.DepthFrom == 20).LithologyId, saved.DepthFromId);
        Assert.Equal(db.Lithologies.Single(l => l.DepthFrom == 30).LithologyId, saved.DepthToId);
    }

    [Fact]
    public async Task Redefining_updates_the_seam_name_column()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(setup.Hole, 100);
        var seam = await service.AddHoleSeamAsync(Layers(data, 0));
        Assert.Equal("", data.Lithology.Single(l => l.DepthFrom == 0).SeamName);

        await service.RedefineHoleSeamAsync(seam, Layers(data, 20));

        Assert.Null(data.Lithology.Single(l => l.DepthFrom == 0).SeamName);
        Assert.Equal("", data.Lithology.Single(l => l.DepthFrom == 20).SeamName);
    }

    [Fact]
    public async Task Redefining_a_seam_that_is_not_on_the_screen_is_rejected()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(setup.Hole, 100);

        await Assert.ThrowsAsync<HoleDataException>(() => service.RedefineHoleSeamAsync(new SeamDef(), Layers(data, 10)));
    }

    [Fact]
    public async Task Overlapping_seam_blocks_the_save()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(setup.Hole, 100);
        await service.AddHoleSeamAsync(Layers(data, 0, 10));
        await service.SaveAsync();

        await service.AddHoleSeamAsync(Layers(data, 10, 20));   // слой 10–20 уже в первом пластопересечении

        var error = await Assert.ThrowsAsync<DataValidationException>(() => service.SaveAsync());
        Assert.Contains(error.Errors, e => e.EndsWith("пересекается с другим пластопересечением этого прослоя."));
    }

    [Fact]
    public async Task Seam_with_the_same_boundaries_is_allowed_in_another_interbed()
    {
        var setup = Seed();
        var service = Service(setup);
        var first = await service.LoadAsync(setup.Hole, 100);
        await service.AddHoleSeamAsync(Layers(first, 10, 20));
        await service.SaveAsync();

        var second = await service.LoadAsync(setup.Hole, 200);
        await service.AddHoleSeamAsync(Layers(second, 10, 20));
        await service.SaveAsync();

        using var db = testDbContext(setup);
        Assert.Equal(2, db.SeamDefs.Count());
    }

    [Fact]
    public async Task A_seam_can_be_deleted_with_the_standard_removal()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(setup.Hole, 100);
        var seam = await service.AddHoleSeamAsync(Layers(data, 10, 20));
        await service.SaveAsync();

        data.HoleSeams.Remove(seam);
        await service.SaveAsync();

        using var db = testDbContext(setup);
        Assert.Empty(db.SeamDefs);
        Assert.Equal(4, db.Lithologies.Count());   // слои остаются
    }

    private static GDB52Context testDbContext(Setup setup) => setup.Db.CreateDbContext();
}
