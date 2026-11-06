using Data.Models;
using Geonicle.Data.Services;
using Geonicle.Validation;
using Xunit;

namespace Geonicle.Data.Tests.Services;

/// <summary>Экран «Блоки подсчёта запасов»: списки пласта и прослоя, блоки версии, варианта и детали с частями и скважинами.</summary>
public class ReserveBlocksServiceTests
{
    private sealed record Setup(TestDb Db, int Seam, int PartA, int PartB, int SeamDefA, int SeamDefB);

    private const int Interbed = 100;
    private const int Version = 1;
    private const int Variant = 111;

    /// <summary>Кондиция: обязательные текстовые поля заполнены, Variant1 (в БД вычисляемое) задаётся явно.</summary>
    private static Condition Cond(int id, int variant, int interbed, string name) => new()
    {
        Id = id, Variant = variant, InterbedId = interbed, Variant1 = name, Variant2 = name,
        IbCond = "", ThCond = "", AshCond = "", ThCondSum = "", AshCondSum = "",
    };

    private static Setup Seed()
    {
        var testDb = new TestDb();
        using var db = testDb.CreateDbContext();
        var seam = new Seam { Description = "Пласт S", ShortName = "S", N = 1, Actual = true };
        var other = new Seam { Description = "Другой", ShortName = "Д", N = 2, Actual = true };
        var holeA = new Borehole { HoleName = "A-1" };
        var holeB = new Borehole { HoleName = "B-2" };
        db.AddRange(seam, other, holeA, holeB);
        db.SaveChanges();

        var partB = new Part { SeamId = seam.SeamId, N = 2, Npart = "2", Description = "" };
        var partA = new Part { SeamId = seam.SeamId, N = 1, Npart = "1", Description = "" };
        var otherPart = new Part { SeamId = other.SeamId, N = 1, Npart = "Ч", Description = "" };
        db.Parts.AddRange(partB, partA, otherPart);

        db.Conditions.AddRange(
            Cond(112, 2, Interbed, "0,5 вар.2"),
            Cond(Variant, 1, Interbed, "0,5 вар.1"),
            Cond(211, 1, 200, "1,0 вар.1"));
        db.SeamConds.AddRange(
            new SeamCond { SeamId = seam.SeamId, ConditionId = Variant, RealCondId = 112 },
            new SeamCond { SeamId = seam.SeamId, ConditionId = 112, RealCondId = null },
            new SeamCond { SeamId = seam.SeamId, ConditionId = 211, RealCondId = null },      // другой прослой
            new SeamCond { SeamId = other.SeamId, ConditionId = Variant, RealCondId = null }); // другой пласт
        db.Versions.AddRange(new global::Data.Models.Version { Id = 1, VersionName = "Основная" }, new global::Data.Models.Version { Id = 2, VersionName = "Альтернативная" });
        db.SaveChanges();

        var layerA = new Lithology { BoreholeId = holeA.BoreholeId, DepthFrom = 10, DepthTo = 12 };
        var layerB = new Lithology { BoreholeId = holeB.BoreholeId, DepthFrom = 30, DepthTo = 32 };
        db.Lithologies.AddRange(layerA, layerB);
        db.SaveChanges();
        var segA = new SeamDef { BoreholeId = holeA.BoreholeId, SeamId = seam.SeamId, InterbedId = Interbed, DepthFromId = layerA.LithologyId, DepthToId = layerA.LithologyId, Comments = "низкий вых." };
        var segB = new SeamDef { BoreholeId = holeB.BoreholeId, SeamId = seam.SeamId, InterbedId = Interbed, DepthFromId = layerB.LithologyId, DepthToId = layerB.LithologyId };
        var segOther = new SeamDef { BoreholeId = holeA.BoreholeId, SeamId = seam.SeamId, InterbedId = 200, DepthFromId = layerA.LithologyId, DepthToId = layerA.LithologyId };
        db.SeamDefs.AddRange(segA, segB, segOther);
        db.SaveChanges();

        var block1 = new Block { BlockName = "1", PartId = partA.PartId, ConditionId = Variant, VersionId = Version, Dip = 18 };
        var block2 = new Block { BlockName = "2", PartId = partA.PartId, ConditionId = Variant, VersionId = Version };
        var elsewhere = new Block { BlockName = "чужой", PartId = partB.PartId, ConditionId = Variant, VersionId = Version };
        db.Blocks.AddRange(block1, block2, elsewhere);
        db.SaveChanges();
        block1.BlockDetails.Add(new BlockDetail { Area = 100 });
        block1.BlockUnits.Add(new BlockUnit { HoleName = "A-1, низкий вых.", HoleSeamId = segA.HoleSeamId });
        block1.BlockUnits.Add(new BlockUnit { HoleName = "изомощность", HoleSeamId = null });
        db.SaveChanges();
        return new Setup(testDb, seam.SeamId, partA.PartId, partB.PartId, segA.HoleSeamId, segB.HoleSeamId);
    }

    [Fact]
    public async Task Lists_hold_the_parts_variants_conditions_and_seam_intersections_of_the_seam_and_interbed()
    {
        var setup = Seed();
        var service = new ReserveBlocksService(setup.Db);

        var lists = await service.LoadListsAsync(setup.Seam, Interbed);

        Assert.Equal(new[] { "1", "2" }, lists.Parts.Select(p => p.Name));   // по N
        Assert.Equal(new[] { Variant, 112 }, lists.Variants.Select(v => v.ConditionId));   // только этот пласт и прослой
        Assert.Equal(new[] { "0,5 вар.1", "0,5 вар.2" }, lists.Variants.Select(v => v.Name));
        Assert.Equal(112, lists.Variants[0].RealCondId);
        Assert.Equal(new[] { "0,5 вар.1", "0,5 вар.2" }, lists.ConditionLookup.Select(c => c.Name));   // кондиции прослоя
        Assert.Equal(new[] { "A-1, низкий вых.", "B-2" }, lists.Holes.Select(h => h.Text));   // без комментария — только скважина
        Assert.Equal(new decimal?[] { 10, 30 }, lists.Holes.Select(h => h.DepthFrom));
    }

    [Fact]
    public async Task Without_a_seam_the_lists_are_empty_and_without_an_interbed_there_are_no_variants_or_holes()
    {
        var setup = Seed();
        var service = new ReserveBlocksService(setup.Db);

        var none = await service.LoadListsAsync(null, Interbed);
        Assert.Empty(none.Parts);
        Assert.Empty(none.Variants);

        var noInterbed = await service.LoadListsAsync(setup.Seam, null);
        Assert.Equal(2, noInterbed.Parts.Count);
        Assert.Empty(noInterbed.Variants);
        Assert.Empty(noInterbed.Holes);
    }

    [Theory]
    [InlineData("A-1", "низкий", "A-1, низкий")]
    [InlineData("A-1", null, "A-1")]
    [InlineData("A-1", "  ", "A-1")]
    [InlineData(null, null, "")]
    public void Hole_text_is_the_borehole_name_and_the_comment(string name, string comments, string expected)
    {
        Assert.Equal(expected, ReserveBlocksService.HoleText(name, comments));
    }

    [Fact]
    public async Task Real_condition_of_a_variant_is_edited_and_saved_with_the_screen()
    {
        var setup = Seed();
        var service = new ReserveBlocksService(setup.Db);
        var lists = await service.LoadListsAsync(setup.Seam, Interbed);

        lists.Variants.Single(v => v.ConditionId == 112).RealCondId = Variant;
        Assert.True(service.HasChanges);
        await service.SaveAsync();

        using var db = setup.Db.CreateDbContext();
        Assert.Equal(Variant, db.SeamConds.Single(s => s.SeamId == setup.Seam && s.ConditionId == 112).RealCondId);
        Assert.Equal(112, db.SeamConds.Single(s => s.SeamId == setup.Seam && s.ConditionId == Variant).RealCondId);   // остальные не тронуты
    }

    [Fact]
    public async Task Blocks_are_those_of_the_version_variant_and_part_with_their_details_and_units()
    {
        var setup = Seed();
        var service = new ReserveBlocksService(setup.Db);
        await service.LoadListsAsync(setup.Seam, Interbed);

        var blocks = await service.LoadBlocksAsync(Version, Variant, setup.PartA);

        Assert.Equal(new[] { "1", "2" }, blocks.Select(b => b.BlockName.Trim()));
        var first = blocks[0];
        Assert.Single(first.BlockDetails);
        Assert.Equal(2, first.BlockUnits.Count);
    }

    [Fact]
    public async Task Units_get_the_depths_of_their_seam_intersection_and_free_text_units_have_none()
    {
        var setup = Seed();
        var service = new ReserveBlocksService(setup.Db);
        await service.LoadListsAsync(setup.Seam, Interbed);

        var blocks = await service.LoadBlocksAsync(Version, Variant, setup.PartA);

        var units = blocks[0].BlockUnits.ToDictionary(u => u.HoleName);
        var inSeam = units["A-1, низкий вых."];
        Assert.Equal((10m, 12m), (inSeam.DepthFrom, inSeam.DepthTo));
        Assert.Null(units["изомощность"].DepthFrom);
        Assert.Null(units["изомощность"].DepthTo);
    }

    [Fact]
    public async Task Nothing_is_loaded_without_version_variant_or_part()
    {
        var setup = Seed();
        var service = new ReserveBlocksService(setup.Db);
        await service.LoadListsAsync(setup.Seam, Interbed);

        Assert.Empty(await service.LoadBlocksAsync(null, Variant, setup.PartA));
        Assert.Empty(await service.LoadBlocksAsync(Version, null, setup.PartA));
        Assert.Empty(await service.LoadBlocksAsync(Version, Variant, null));
    }

    [Fact]
    public async Task Added_block_gets_the_version_variant_and_part_with_the_column_defaults_and_is_saved()
    {
        var setup = Seed();
        var service = new ReserveBlocksService(setup.Db);
        await service.LoadListsAsync(setup.Seam, Interbed);
        var blocks = await service.LoadBlocksAsync(Version, Variant, setup.PartA);

        blocks.Add(new Block { BlockName = "новый" });
        await service.SaveAsync();

        using var db = setup.Db.CreateDbContext();
        var saved = db.Blocks.Single(b => b.BlockName == "новый");
        Assert.Equal((Version, Variant, setup.PartA), (saved.VersionId, saved.ConditionId, saved.PartId));
        Assert.Equal((BlockCategory.A, 5, true), (saved.Category, saved.CoalType, saved.Balance));
        Assert.True(saved.BlockId > 0);
    }

    [Fact]
    public async Task A_block_without_a_name_or_with_a_long_name_blocks_the_save()
    {
        var setup = Seed();
        var service = new ReserveBlocksService(setup.Db);
        await service.LoadListsAsync(setup.Seam, Interbed);
        var blocks = await service.LoadBlocksAsync(Version, Variant, setup.PartA);

        var block = new Block { BlockName = "  " };
        blocks.Add(block);
        var empty = await Assert.ThrowsAsync<DataValidationException>(() => service.SaveAsync());
        Assert.Contains(empty.Errors, e => e.EndsWith("Название блока обязательно."));

        block.BlockName = "очень длинное название";
        var tooLong = await Assert.ThrowsAsync<DataValidationException>(() => service.SaveAsync());
        Assert.Contains(tooLong.Errors, e => e.EndsWith("не может быть длиннее 12 символов."));
    }

    [Fact]
    public async Task Removed_block_is_deleted_with_its_details_and_units()
    {
        var setup = Seed();
        var service = new ReserveBlocksService(setup.Db);
        await service.LoadListsAsync(setup.Seam, Interbed);
        var blocks = await service.LoadBlocksAsync(Version, Variant, setup.PartA);

        blocks.Remove(blocks[0]);
        await service.SaveAsync();

        using var db = setup.Db.CreateDbContext();
        Assert.DoesNotContain(db.Blocks, b => b.BlockName == "1");
        Assert.Empty(db.BlockDetails);
        Assert.Empty(db.BlockUnits);
        Assert.Contains(db.Blocks, b => b.BlockName == "чужой");
    }

    [Fact]
    public async Task Edits_survive_switching_to_another_part_and_are_saved_together()
    {
        var setup = Seed();
        var service = new ReserveBlocksService(setup.Db);
        await service.LoadListsAsync(setup.Seam, Interbed);
        var partA = await service.LoadBlocksAsync(Version, Variant, setup.PartA);
        partA[0].Comments = "проверено";
        partA[0].Dip = 25;

        await service.LoadBlocksAsync(Version, Variant, setup.PartB);   // выбрали другую деталь
        Assert.True(service.HasChanges);
        await service.SaveAsync();

        using var db = setup.Db.CreateDbContext();
        var saved = db.Blocks.Single(b => b.BlockName == "1");
        Assert.Equal(("проверено", 25m), (saved.Comments, saved.Dip));
    }

    [Fact]
    public async Task Added_detail_and_unit_get_the_block_and_version_and_the_column_defaults()
    {
        var setup = Seed();
        var service = new ReserveBlocksService(setup.Db);
        await service.LoadListsAsync(setup.Seam, Interbed);
        var blocks = await service.LoadBlocksAsync(Version, Variant, setup.PartA);

        blocks[1].BlockDetails.Add(new BlockDetail { Area = 50 });
        blocks[1].BlockUnits.Add(new BlockUnit { HoleName = "B-2" });
        await service.SaveAsync();

        using var db = setup.Db.CreateDbContext();
        var blockId = db.Blocks.Single(b => b.BlockName == "2").BlockId;
        var detail = db.BlockDetails.Single(d => d.BlockId == blockId);
        Assert.Equal((Version, 100, 12), (detail.VersionId, detail.Boundary, detail.Pillar));
        var unit = db.BlockUnits.Single(u => u.BlockId == blockId);
        Assert.Equal((Version, true, true), (unit.VersionId, unit.AdR, unit.ThR));
    }

    [Fact]
    public async Task Choosing_a_seam_intersection_by_text_sets_the_seam_id_and_depths_and_free_text_clears_them()
    {
        var setup = Seed();
        var service = new ReserveBlocksService(setup.Db);
        await service.LoadListsAsync(setup.Seam, Interbed);
        var unit = new BlockUnit { HoleName = "A-1, низкий вых." };

        service.ApplyHole(unit);
        Assert.Equal(setup.SeamDefA, unit.HoleSeamId);
        Assert.Equal((10m, 12m), (unit.DepthFrom.Value, unit.DepthTo.Value));

        unit.HoleName = "изомощность";
        service.ApplyHole(unit);
        Assert.Null(unit.HoleSeamId);
        Assert.Null(unit.DepthFrom);
    }

    [Fact]
    public async Task A_unit_without_a_name_or_with_a_name_over_50_characters_blocks_the_save()
    {
        var setup = Seed();
        var service = new ReserveBlocksService(setup.Db);
        await service.LoadListsAsync(setup.Seam, Interbed);
        var blocks = await service.LoadBlocksAsync(Version, Variant, setup.PartA);
        var unit = new BlockUnit { HoleName = "" };
        blocks[1].BlockUnits.Add(unit);

        var empty = await Assert.ThrowsAsync<DataValidationException>(() => service.SaveAsync());
        Assert.Contains(empty.Errors, e => e.EndsWith("Название скважины в блоке обязательно."));

        unit.HoleName = new string('х', 51);
        var tooLong = await Assert.ThrowsAsync<DataValidationException>(() => service.SaveAsync());
        Assert.Contains(tooLong.Errors, e => e.EndsWith("не может быть длиннее 50 символов."));
    }

    [Fact]
    public async Task A_detail_with_a_negative_area_blocks_the_save()
    {
        var setup = Seed();
        var service = new ReserveBlocksService(setup.Db);
        await service.LoadListsAsync(setup.Seam, Interbed);
        var blocks = await service.LoadBlocksAsync(Version, Variant, setup.PartA);

        blocks[0].BlockDetails.Single().Area = -1;

        var error = await Assert.ThrowsAsync<DataValidationException>(() => service.SaveAsync());
        Assert.Contains(error.Errors, e => e.EndsWith("Площадь не может быть отрицательной."));
    }

    [Fact]
    public async Task Versions_and_dictionary_lookups_are_loaded()
    {
        var testDb = new TestDb();
        using (var db = testDb.CreateDbContext())
        {
            db.Versions.AddRange(new global::Data.Models.Version { Id = 2, VersionName = "Б" }, new global::Data.Models.Version { Id = 1, VersionName = "А" });
            db.Dictionaries.AddRange(
                new Dictionary { Section = "тип угля", N = 1, Description = "Т" },
                new Dictionary { Section = "марка", N = 1, Description = "Г" },
                new Dictionary { Section = "boundary detail", N = 1, Description = "опт лицензия" },
                new Dictionary { Section = "pillar", N = 1, Description = "Вне целиков" },
                new Dictionary { Section = "другое", N = 1, Description = "не берём" });
            db.SaveChanges();
        }
        var service = new ReserveBlocksService(testDb);
        await service.LoadListsAsync(null, null);

        Assert.Equal(new[] { "А", "Б" }, (await service.GetVersionsAsync()).Select(v => v.Name));
        Assert.Equal(new[] { "Т" }, (await service.GetCoalTypeLookupAsync()).Select(i => i.Name));
        Assert.Equal(new[] { "Г" }, (await service.GetCoalGradeLookupAsync()).Select(i => i.Name));
        Assert.Equal(new[] { "опт лицензия" }, (await service.GetBoundaryLookupAsync()).Select(i => i.Name));
        Assert.Equal(new[] { "Вне целиков" }, (await service.GetPillarLookupAsync()).Select(i => i.Name));
    }
}
