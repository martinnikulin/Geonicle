using Data.Models;
using Geonicle.Data.Services;
using Geonicle.Validation;
using Xunit;

namespace Geonicle.Data.Tests.Services;

/// <summary>Экран «Пластопересечения»: пластопересечения и средние пласта, литология с «В подсчёт» и пробы выбранного пластопересечения.</summary>
public class SeamIntersectionsServiceTests
{
    private sealed record Setup(TestDb Db, int Seam, int OtherSeam, int HoleA, int HoleB, int SeamDefA, int SeamDefB);

    private const int Interbed = 100;

    /// <summary>
    /// Пласт S: пластопересечение в скважине A (слои 0–10 и 10–20, пласт 0–20) и в B (слой 0–50, пласт 0–50) в прослое 100,
    /// ещё одно в A в прослое 200 и у другого пласта. У слоёв A есть LithRes, у слоя «20–30» нет.
    /// </summary>
    private static Setup Seed()
    {
        var testDb = new TestDb();
        using var db = testDb.CreateDbContext();
        var seam = new Seam { Description = "Пласт S", ShortName = "S", N = 1, Actual = true };
        var other = new Seam { Description = "Другой", ShortName = "Д", N = 2, Actual = true };
        var holeB = new Borehole { HoleName = "B-2" };
        var holeA = new Borehole { HoleName = "A-1" };
        db.AddRange(seam, other, holeB, holeA);
        db.SaveChanges();

        var a1 = new Lithology { BoreholeId = holeA.BoreholeId, DepthFrom = 0, DepthTo = 10, ThNorm = 1 };
        var a2 = new Lithology { BoreholeId = holeA.BoreholeId, DepthFrom = 10, DepthTo = 20, ThNorm = 2 };
        var a3 = new Lithology { BoreholeId = holeA.BoreholeId, DepthFrom = 20, DepthTo = 30, ThNorm = 3 };   // вне пластопересечения
        var b1 = new Lithology { BoreholeId = holeB.BoreholeId, DepthFrom = 0, DepthTo = 50 };
        db.Lithologies.AddRange(a1, a2, a3, b1);
        db.SaveChanges();
        db.LithRes.AddRange(
            new LithRe { LithologyId = a1.LithologyId, InterbedId = Interbed, Reserves = true },
            new LithRe { LithologyId = a2.LithologyId, InterbedId = Interbed, Reserves = false },
            new LithRe { LithologyId = a1.LithologyId, InterbedId = 200, Reserves = false });   // другой прослой

        var segA = new SeamDef { BoreholeId = holeA.BoreholeId, SeamId = seam.SeamId, InterbedId = Interbed, DepthFromId = a1.LithologyId, DepthToId = a2.LithologyId };
        var segB = new SeamDef { BoreholeId = holeB.BoreholeId, SeamId = seam.SeamId, InterbedId = Interbed, DepthFromId = b1.LithologyId, DepthToId = b1.LithologyId };
        db.SeamDefs.AddRange(
            segA, segB,
            new SeamDef { BoreholeId = holeA.BoreholeId, SeamId = seam.SeamId, InterbedId = 200, DepthFromId = a1.LithologyId, DepthToId = a1.LithologyId },
            new SeamDef { BoreholeId = holeA.BoreholeId, SeamId = other.SeamId, InterbedId = Interbed, DepthFromId = a3.LithologyId, DepthToId = a3.LithologyId });

        db.Averages.AddRange(
            new Average { InterbedId = Interbed, SeamId = seam.SeamId, CoalType = 1, CoalGrade = 2, VarId = 20, Value = 1, DateCreated = DateTime.Today },
            new Average { InterbedId = Interbed, SeamId = seam.SeamId, CoalType = 1, CoalGrade = 2, VarId = 10, Value = 2, DateCreated = DateTime.Today },
            new Average { InterbedId = 200, SeamId = seam.SeamId, CoalType = 1, CoalGrade = 2, VarId = 10, Value = 3 },           // другой прослой
            new Average { InterbedId = Interbed, SeamId = other.SeamId, CoalType = 1, CoalGrade = 2, VarId = 10, Value = 4 });     // другой пласт
        db.SaveChanges();

        // Пробы в A: 5–6 (внутри), 19–21 (пересекает границу 20), 25–26 (вне); связи с пластопересечением A для первых двух.
        var s1 = new SampleDef { BoreholeId = holeA.BoreholeId, DepthFrom = 5, DepthTo = 6, Nsample = "P1" };
        var s2 = new SampleDef { BoreholeId = holeA.BoreholeId, DepthFrom = 19, DepthTo = 21, Nsample = "P2" };
        var s3 = new SampleDef { BoreholeId = holeA.BoreholeId, DepthFrom = 25, DepthTo = 26, Nsample = "P3" };
        db.SampleDefs.AddRange(s1, s2, s3);
        db.SaveChanges();
        db.Hssamps.AddRange(
            new Hssamp { HoleSeamId = segA.HoleSeamId, SampleId = s1.SampleId },
            new Hssamp { HoleSeamId = segA.HoleSeamId, SampleId = s2.SampleId });
        db.SaveChanges();
        return new Setup(testDb, seam.SeamId, other.SeamId, holeA.BoreholeId, holeB.BoreholeId, segA.HoleSeamId, segB.HoleSeamId);
    }

    [Fact]
    public async Task Seam_intersections_of_the_seam_and_interbed_are_loaded_ordered_by_borehole_name()
    {
        var setup = Seed();
        var service = new SeamIntersectionsService(setup.Db);

        var data = await service.LoadAsync(setup.Seam, Interbed);

        Assert.Equal(new[] { "A-1", "B-2" }, data.HoleSeams.Select(s => s.Borehole.HoleName));
        Assert.Equal(new decimal?[] { 0, 0 }, data.HoleSeams.Select(s => s.DepthFrom));
        Assert.Equal(new decimal?[] { 20, 50 }, data.HoleSeams.Select(s => s.DepthTo));
    }

    [Fact]
    public async Task Averages_are_limited_to_the_seam_and_interbed_and_ordered_by_key()
    {
        var setup = Seed();
        var data = await new SeamIntersectionsService(setup.Db).LoadAsync(setup.Seam, Interbed);

        Assert.Equal(new[] { 10, 20 }, data.Averages.Select(a => a.VarId));
        Assert.All(data.Averages, a => Assert.Equal(setup.Seam, a.SeamId));
    }

    [Fact]
    public async Task Nothing_is_loaded_without_a_seam_or_interbed()
    {
        var setup = Seed();
        var service = new SeamIntersectionsService(setup.Db);

        foreach (var data in new[] { await service.LoadAsync(null, Interbed), await service.LoadAsync(setup.Seam, null) })
        {
            Assert.Empty(data.HoleSeams);
            Assert.Empty(data.Averages);
        }
    }

    [Fact]
    public async Task Seam_intersection_fields_are_edited_and_saved()
    {
        var setup = Seed();
        var service = new SeamIntersectionsService(setup.Db);
        var data = await service.LoadAsync(setup.Seam, Interbed);

        var first = data.HoleSeams.First();
        first.Comments = "проверено";
        first.Kb = KbType.КБ;
        first.Lock = true;
        await service.SaveAsync();

        using var db = setup.Db.CreateDbContext();
        var saved = db.SeamDefs.Single(s => s.HoleSeamId == setup.SeamDefA);
        Assert.Equal(("проверено", KbType.КБ, true), (saved.Comments, saved.Kb, saved.Lock));
    }

    [Fact]
    public async Task Average_user_value_is_edited_and_saved()
    {
        var setup = Seed();
        var service = new SeamIntersectionsService(setup.Db);
        var data = await service.LoadAsync(setup.Seam, Interbed);

        data.Averages.Single(a => a.VarId == 10).UserValue = 7.5;
        await service.SaveAsync();

        using var db = setup.Db.CreateDbContext();
        Assert.Equal(7.5, db.Averages.Single(a => a.SeamId == setup.Seam && a.InterbedId == Interbed && a.VarId == 10).UserValue);
    }

    [Fact]
    public async Task Added_average_gets_the_seam_and_interbed_and_is_saved()
    {
        var setup = Seed();
        var service = new SeamIntersectionsService(setup.Db);
        var data = await service.LoadAsync(setup.Seam, Interbed);

        data.Averages.Add(new Average { CoalType = 2, CoalGrade = 3, VarId = 30, UserValue = 1 });
        await service.SaveAsync();

        using var db = setup.Db.CreateDbContext();
        var saved = db.Averages.Single(a => a.VarId == 30);
        Assert.Equal((setup.Seam, Interbed, 2, 3), (saved.SeamId, saved.InterbedId, saved.CoalType, saved.CoalGrade));
    }

    [Fact]
    public async Task Average_without_a_variable_or_with_a_duplicate_key_blocks_the_save()
    {
        var setup = Seed();
        var service = new SeamIntersectionsService(setup.Db);
        var data = await service.LoadAsync(setup.Seam, Interbed);

        data.Averages.Add(new Average { CoalType = 1, CoalGrade = 2, VarId = 0 });
        var noVar = await Assert.ThrowsAsync<DataValidationException>(() => service.SaveAsync());
        Assert.Contains(noVar.Errors, e => e.EndsWith("Выберите показатель."));

        data.Averages.Last().VarId = 10;   // такая комбинация уже есть: EF не может отслеживать две строки с одним ключом
        var duplicate = await Assert.ThrowsAsync<DataValidationException>(() => service.SaveAsync());
        Assert.Contains(duplicate.Errors, e => e.StartsWith("Две строки совпадают по ключу"));
    }

    [Fact]
    public async Task Removed_average_is_deleted()
    {
        var setup = Seed();
        var service = new SeamIntersectionsService(setup.Db);
        var data = await service.LoadAsync(setup.Seam, Interbed);

        data.Averages.Remove(data.Averages.Single(a => a.VarId == 20));
        await service.SaveAsync();

        using var db = setup.Db.CreateDbContext();
        Assert.DoesNotContain(db.Averages, a => a.SeamId == setup.Seam && a.InterbedId == Interbed && a.VarId == 20);
        Assert.Contains(db.Averages, a => a.InterbedId == 200);   // чужие не тронуты
    }

    [Fact]
    public void Saved_average_is_not_new_and_an_unsaved_one_is()
    {
        Assert.True(new Average().IsNew);
        Assert.False(new Average { DateCreated = DateTime.Today }.IsNew);
    }

    [Fact]
    public async Task Lithology_is_the_layers_fully_inside_the_seam_intersection_with_the_interbed_reserves_flag()
    {
        var setup = Seed();
        var service = new SeamIntersectionsService(setup.Db);
        var data = await service.LoadAsync(setup.Seam, Interbed);

        var details = await service.LoadDetailsAsync(data.HoleSeams.First(s => s.BoreholeId == setup.HoleA));

        Assert.Equal(new decimal[] { 0, 10 }, details.Lithology.Select(l => l.DepthFrom));   // слой 20–30 вне пластопересечения
        Assert.Equal(new bool?[] { true, false }, details.Lithology.Select(l => l.Reserves));   // LithRes выбранного прослоя, не другого
    }

    [Fact]
    public async Task A_layer_without_a_LithRes_row_has_no_reserves_value_and_cannot_be_changed()
    {
        var setup = Seed();
        var service = new SeamIntersectionsService(setup.Db);
        var data = await service.LoadAsync(setup.Seam, Interbed);

        var details = await service.LoadDetailsAsync(data.HoleSeams.First(s => s.BoreholeId == setup.HoleB));   // у слоя B строки LithRes нет
        var layer = Assert.Single(details.Lithology);
        layer.Reserves = true;

        Assert.Null(layer.Reserves);
        Assert.False(service.HasChanges);
    }

    [Fact]
    public async Task Layer_fields_and_reserves_flag_are_saved()
    {
        var setup = Seed();
        var service = new SeamIntersectionsService(setup.Db);
        var data = await service.LoadAsync(setup.Seam, Interbed);
        var details = await service.LoadDetailsAsync(data.HoleSeams.First(s => s.BoreholeId == setup.HoleA));

        var layer = details.Lithology.First();
        layer.ThNorm = 9;
        layer.Reserves = false;
        await service.SaveAsync();

        using var db = setup.Db.CreateDbContext();
        var saved = db.Lithologies.Single(l => l.LithologyId == layer.LithologyId);
        Assert.Equal(9, saved.ThNorm);
        Assert.False(db.LithRes.Single(r => r.LithologyId == layer.LithologyId && r.InterbedId == Interbed).Reserves);
        Assert.False(db.LithRes.Single(r => r.LithologyId == layer.LithologyId && r.InterbedId == 200).Reserves);   // другой прослой не тронут
    }

    [Fact]
    public async Task Samples_are_those_overlapping_the_seam_intersection_numbered_by_depth()
    {
        var setup = Seed();
        var service = new SeamIntersectionsService(setup.Db);
        var data = await service.LoadAsync(setup.Seam, Interbed);

        var details = await service.LoadDetailsAsync(data.HoleSeams.First(s => s.BoreholeId == setup.HoleA));

        Assert.Equal(new[] { "P1", "P2" }, details.Samples.Select(r => r.Nsample));   // P2 пересекает границу 20, P3 вне
        Assert.Equal(new[] { 1, 2 }, details.Samples.Select(r => r.N));
        Assert.All(details.Samples, r => Assert.True(r.HasSeamLink));
    }

    [Fact]
    public async Task Accepted_is_saved_to_the_link_of_the_shown_seam_intersection()
    {
        var setup = Seed();
        var service = new SeamIntersectionsService(setup.Db);
        var data = await service.LoadAsync(setup.Seam, Interbed);
        var details = await service.LoadDetailsAsync(data.HoleSeams.First(s => s.BoreholeId == setup.HoleA));

        details.Samples.Single(r => r.Nsample == "P2").Accepted = true;
        details.Samples.Single(r => r.Nsample == "P1").Bad = true;
        await service.SaveAsync();

        using var db = setup.Db.CreateDbContext();
        Assert.True(db.Hssamps.Single(h => h.HoleSeamId == setup.SeamDefA && h.SampleDef.Nsample == "P2").Accepted);
        Assert.False(db.Hssamps.Single(h => h.HoleSeamId == setup.SeamDefA && h.SampleDef.Nsample == "P1").Accepted);
        Assert.True(db.SampleDefs.Single(s => s.Nsample == "P1").Bad);
    }

    [Fact]
    public async Task Without_a_seam_intersection_there_are_no_layers_and_samples()
    {
        var setup = Seed();
        var service = new SeamIntersectionsService(setup.Db);
        await service.LoadAsync(setup.Seam, Interbed);

        var details = await service.LoadDetailsAsync(null);

        Assert.Empty(details.Lithology);
        Assert.Empty(details.Samples);
    }

    [Fact]
    public async Task Unsaved_edits_survive_switching_to_another_seam_intersection()
    {
        var setup = Seed();
        var service = new SeamIntersectionsService(setup.Db);
        var data = await service.LoadAsync(setup.Seam, Interbed);
        var inA = await service.LoadDetailsAsync(data.HoleSeams.First(s => s.BoreholeId == setup.HoleA));
        inA.Lithology.First().ThNorm = 42;

        await service.LoadDetailsAsync(data.HoleSeams.First(s => s.BoreholeId == setup.HoleB));   // выбрали другую строку
        Assert.True(service.HasChanges);
        await service.SaveAsync();

        using var db = setup.Db.CreateDbContext();
        Assert.Equal(42, db.Lithologies.Single(l => l.LithologyId == inA.Lithology.First().LithologyId).ThNorm);
    }

    [Fact]
    public async Task Links_created_by_triggers_are_picked_up_after_saving()
    {
        var setup = Seed();
        var service = new SeamIntersectionsService(setup.Db);
        var data = await service.LoadAsync(setup.Seam, Interbed);
        var details = await service.LoadDetailsAsync(data.HoleSeams.First(s => s.BoreholeId == setup.HoleA));
        var p1 = details.Samples.Single(r => r.Nsample == "P1");
        using (var db = setup.Db.CreateDbContext())   // «триггер»: связь P1 с пластопересечением исчезла
        {
            db.Hssamps.Remove(db.Hssamps.Single(h => h.HoleSeamId == setup.SeamDefA && h.SampleDef.Nsample == "P1"));
            db.SaveChanges();
        }

        await service.RefreshAfterSaveAsync();

        Assert.False(p1.HasSeamLink);
    }

    [Fact]
    public async Task Lookups_come_from_the_dictionary_and_rocks()
    {
        var testDb = new TestDb();
        using (var db = testDb.CreateDbContext())
        {
            db.Dictionaries.AddRange(
                new Dictionary { Section = "марка", N = 2, Description = "Ж" },
                new Dictionary { Section = "марка", N = 1, Description = "Г" },
                new Dictionary { Section = "тип угля", N = 1, Description = "Т" },
                new Dictionary { Section = "Ad", N = 2, Description = "Ad флот" },
                new Dictionary { Section = "Ad", N = 1, Description = "Ad" });
            db.SaveChanges();
        }
        var service = new SeamIntersectionsService(testDb);
        await service.LoadAsync(null, null);

        Assert.Equal(new[] { "Г", "Ж" }, (await service.GetCoalGradeLookupAsync()).Select(i => i.Name));
        Assert.Equal(new[] { "Т" }, (await service.GetCoalTypeLookupAsync()).Select(i => i.Name));
        Assert.Equal(new[] { (1, "Ad"), (2, "Ad флот") }, (await service.GetAdIndexLookupAsync()).Select(i => (i.Id, i.Name)));
        // Показатели и породы — представления без ключа: в InMemory их не заполнить, проверяем только что запросы выполняются.
        Assert.Empty(await service.GetAssayVarLookupAsync());
        Assert.Empty(await service.GetRockLookupAsync());
    }
}
