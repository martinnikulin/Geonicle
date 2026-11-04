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

/// <summary>Грид «Пробы»: загрузка, связь с пластопересечением, правки, добавление и удаление, нумерация.</summary>
public class SampleRowsTests
{
    private sealed record Setup(TestDb Db, int Hole, int SeamA, int SeamB);

    /// <summary>Скважина с тремя пробами (A 10–11, B 20–21, C 30–31); связи HSSamps: A—пласт A, B—пласт A, B—пласт B.</summary>
    private static Setup Seed()
    {
        var testDb = new TestDb();
        using var db = testDb.CreateDbContext();
        var seam = new Seam { Description = "Пласт 1", ShortName = "П1" };
        var hole = new Borehole { HoleName = "A" };
        var otherHole = new Borehole { HoleName = "B" };
        db.AddRange(seam, hole, otherHole);
        db.SaveChanges();
        // Пластопересечение A: 0–25 (пробы A и B целиком внутри), B: 15–100 (пробы B и C), прослои разные.
        var layer1 = new Lithology { BoreholeId = hole.BoreholeId, DepthFrom = 0, DepthTo = 25 };
        var layer2 = new Lithology { BoreholeId = hole.BoreholeId, DepthFrom = 15, DepthTo = 100 };
        db.Lithologies.AddRange(layer1, layer2);
        db.SaveChanges();
        var seamA = new SeamDef { BoreholeId = hole.BoreholeId, SeamId = seam.SeamId, InterbedId = 100, DepthFromId = layer1.LithologyId, DepthToId = layer1.LithologyId };
        var seamB = new SeamDef { BoreholeId = hole.BoreholeId, SeamId = seam.SeamId, InterbedId = 200, DepthFromId = layer2.LithologyId, DepthToId = layer2.LithologyId };
        db.SeamDefs.AddRange(seamA, seamB);
        var s3 = new SampleDef { BoreholeId = hole.BoreholeId, DepthFrom = 30, DepthTo = 31, Nsample = "C", AdIndex = 1 };   // добавлена первой, глубже всех
        var s1 = new SampleDef { BoreholeId = hole.BoreholeId, DepthFrom = 10, DepthTo = 11, Nsample = "A", AdIndex = 1 };
        var s2 = new SampleDef { BoreholeId = hole.BoreholeId, DepthFrom = 20, DepthTo = 21, Nsample = "B", AdIndex = 2 };
        var foreign = new SampleDef { BoreholeId = otherHole.BoreholeId, DepthFrom = 1, DepthTo = 2, Nsample = "чужая" };
        db.SampleDefs.AddRange(s3, s1, s2, foreign);
        db.SaveChanges();
        db.Hssamps.AddRange(
            new Hssamp { HoleSeamId = seamA.HoleSeamId, SampleId = s1.SampleId },
            new Hssamp { HoleSeamId = seamA.HoleSeamId, SampleId = s2.SampleId },
            new Hssamp { HoleSeamId = seamB.HoleSeamId, SampleId = s2.SampleId });
        db.SaveChanges();
        return new Setup(testDb, hole.BoreholeId, seamA.HoleSeamId, seamB.HoleSeamId);
    }

    private static HoleDataService Service(Setup s) => new(s.Db);

    [Fact]
    public async Task Samples_of_the_borehole_are_loaded_ordered_by_depth()
    {
        var setup = Seed();
        var data = await Service(setup).LoadAsync(setup.Hole, 100);

        Assert.Equal(new[] { "A", "B", "C" }, data.Samples.Select(r => r.Nsample));
    }

    [Fact]
    public async Task Nothing_is_loaded_for_samples_without_a_borehole()
    {
        var setup = Seed();
        var data = await Service(setup).LoadAsync(null, 100);
        Assert.Empty(data.Samples);
    }

    [Fact]
    public async Task Accepted_follows_the_selected_seam_and_samples_without_a_link_are_not_accepted()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(setup.Hole, 100);
        data.Samples.Single(r => r.Nsample == "A").Def.Hssamps.Single().Accepted = true;

        service.UpdateSampleView(setup.SeamA, bySeam: true);

        Assert.True(data.Samples.Single(r => r.Nsample == "A").Accepted);
        Assert.False(data.Samples.Single(r => r.Nsample == "B").Accepted);
        Assert.Equal(new[] { true, true, false }, data.Samples.Select(r => r.HasSeamLink));   // A, B, C
    }

    [Fact]
    public async Task Samples_are_numbered_by_depth_among_the_shown_ones()
    {
        var setup = Seed();
        var service = Service(setup);
        var first = await service.LoadAsync(setup.Hole, 100);   // прослой 100: пластопересечение A (0–25)

        service.UpdateSampleView(setup.SeamA, bySeam: false);   // вся скважина
        Assert.Equal(new[] { 1, 2, 3 }, first.Samples.Select(r => r.N));

        service.UpdateSampleView(setup.SeamA, bySeam: true);    // пробы целиком внутри 0–25: A и B
        Assert.Equal(new[] { 1, 2, 0 }, first.Samples.Select(r => r.N));

        var second = await service.LoadAsync(setup.Hole, 200);  // прослой 200: пластопересечение B (15–100)
        service.UpdateSampleView(setup.SeamB, bySeam: true);    // пробы B и C
        Assert.Equal(new[] { 0, 1, 2 }, second.Samples.Select(r => r.N));
    }

    [Fact]
    public async Task A_sample_partly_inside_the_seam_is_shown_but_one_outside_is_not()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(setup.Hole, 100);   // пластопересечение A: 0–25
        var b = data.Samples.Single(r => r.Nsample == "B");

        b.DepthFrom = 24;
        b.DepthTo = 26;   // выходит за границу, но пересекается с пластом
        service.UpdateSampleView(setup.SeamA, bySeam: true);
        Assert.Equal(new[] { 1, 2, 0 }, data.Samples.Select(r => r.N));

        b.DepthFrom = 25;
        b.DepthTo = 26;   // касается границы, но не пересекается (как Samples.DepthFrom < DepthTo в триггере)
        service.UpdateSampleView(setup.SeamA, bySeam: true);
        Assert.Equal(new[] { 1, 0, 0 }, data.Samples.Select(r => r.N));
    }

    [Fact]
    public async Task Accepted_edit_is_saved_to_the_link_of_the_selected_seam_only()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(setup.Hole, 100);
        service.UpdateSampleView(setup.SeamA, bySeam: true);

        data.Samples.Single(r => r.Nsample == "B").Accepted = true;
        await service.SaveAsync();

        using var db = setup.Db.CreateDbContext();
        Assert.True(db.Hssamps.Single(h => h.HoleSeamId == setup.SeamA && h.SampleDef.Nsample == "B").Accepted);
        Assert.False(db.Hssamps.Single(h => h.HoleSeamId == setup.SeamB).Accepted);
    }

    [Fact]
    public async Task Accepted_cannot_be_set_for_a_sample_without_a_link()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(setup.Hole, 100);
        service.UpdateSampleView(setup.SeamA, bySeam: true);

        data.Samples.Single(r => r.Nsample == "C").Accepted = true;

        Assert.False(data.Samples.Single(r => r.Nsample == "C").Accepted);
        Assert.False(service.HasChanges);
    }

    [Fact]
    public async Task Sample_fields_are_saved_through_SampleDef()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(setup.Hole, 100);
        var row = data.Samples.Single(r => r.Nsample == "A");

        row.Bad = true;
        row.Nsample = "A2";
        row.DepthFrom = 12;
        row.DepthTo = 13;
        row.AdIndex = 3;
        await service.SaveAsync();

        using var db = setup.Db.CreateDbContext();
        var saved = db.SampleDefs.Single(s => s.SampleId == row.SampleId);
        Assert.True(saved.Bad);
        Assert.Equal("A2", saved.Nsample);
        Assert.Equal((12m, 13m, 3), (saved.DepthFrom, saved.DepthTo, saved.AdIndex));
    }

    [Fact]
    public async Task Invalid_sample_blocks_the_save()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(setup.Hole, 100);

        data.Samples.First().DepthTo = -5;

        var error = await Assert.ThrowsAsync<DataValidationException>(() => service.SaveAsync());
        Assert.Contains(error.Errors, e => e.EndsWith("Глубина «До» должна быть больше «От»."));
    }

    [Fact]
    public async Task Added_sample_gets_the_borehole_and_defaults_and_is_saved_in_borehole_mode()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(setup.Hole, 100);
        service.UpdateSampleView(setup.SeamA, bySeam: false);

        var row = new SampleRow { DepthFrom = 50, DepthTo = 51, Nsample = "новая" };
        data.Samples.Add(row);
        await service.SaveAsync();

        using var db = setup.Db.CreateDbContext();
        var saved = db.SampleDefs.Single(s => s.Nsample == "новая");
        Assert.Equal(setup.Hole, saved.BoreholeId);
        Assert.Equal(1, saved.AdIndex);   // как DF_Samples_Ad_index
        Assert.True(saved.Show);
        Assert.True(row.SampleId > 0);
    }

    [Fact]
    public async Task Removed_sample_is_deleted_in_borehole_mode()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(setup.Hole, 100);
        service.UpdateSampleView(null, bySeam: false);

        data.Samples.Remove(data.Samples.Single(r => r.Nsample == "C"));
        await service.SaveAsync();

        using var db = setup.Db.CreateDbContext();
        Assert.DoesNotContain(db.SampleDefs, s => s.Nsample == "C");
    }

    [Fact]
    public async Task Adding_and_removing_samples_is_ignored_in_seam_mode()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(setup.Hole, 100);
        service.UpdateSampleView(setup.SeamA, bySeam: true);

        data.Samples.Add(new SampleRow { DepthFrom = 50, DepthTo = 51, Nsample = "лишняя" });
        data.Samples.Remove(data.Samples.Single(r => r.Nsample == "A"));

        Assert.False(service.HasChanges);
        await service.SaveAsync();
        using var db = setup.Db.CreateDbContext();
        Assert.DoesNotContain(db.SampleDefs, s => s.Nsample == "лишняя");
        Assert.Contains(db.SampleDefs, s => s.Nsample == "A");
    }

    [Fact]
    public async Task Links_added_and_removed_by_database_triggers_are_picked_up_after_saving()
    {
        var setup = Seed();
        var service = Service(setup);
        var data = await service.LoadAsync(setup.Hole, 100);
        var sampleC = data.Samples.Single(r => r.Nsample == "C");
        service.UpdateSampleView(setup.SeamA, bySeam: true);
        Assert.False(sampleC.HasSeamLink);

        using (var db = setup.Db.CreateDbContext())   // «триггер»: связь C с A появилась, связь B с A исчезла
        {
            db.Hssamps.Add(new Hssamp { HoleSeamId = setup.SeamA, SampleId = sampleC.SampleId });
            db.Hssamps.Remove(db.Hssamps.Single(h => h.HoleSeamId == setup.SeamA && h.SampleDef.Nsample == "B"));
            db.SaveChanges();
        }
        await service.RefreshSamplesAsync();
        service.UpdateSampleView(setup.SeamA, bySeam: true);

        Assert.Equal(new[] { true, false, true }, data.Samples.Select(r => r.HasSeamLink));   // A, B, C
    }

    [Fact]
    public async Task Ad_index_lookup_uses_the_dictionary_number_not_the_key()
    {
        var testDb = new TestDb();
        using (var db = testDb.CreateDbContext())
        {
            db.Dictionaries.AddRange(
                new Dictionary { Section = "Ad", N = 2, Description = "Ad флот" },
                new Dictionary { Section = "Ad", N = 1, Description = "Ad" },
                new Dictionary { Section = "марка", N = 1, Description = "не берём" });
            db.SaveChanges();
        }
        var service = new HoleDataService(testDb);
        await service.LoadAsync(null, null);

        var items = await service.GetAdIndexLookupAsync();

        Assert.Equal(new[] { (1, "Ad"), (2, "Ad флот") }, items.Select(i => (i.Id, i.Name)));
    }
}
