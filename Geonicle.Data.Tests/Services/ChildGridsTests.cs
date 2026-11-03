using Data.Models;
using Geonicle.Data.Services;
using Geonicle.Validation;
using Xunit;

namespace Geonicle.Data.Tests.Services;

/// <summary>Зависимые коллекции: удаление строки из коллекции родителя и проверка при сохранении.</summary>
public class ChildGridsTests
{
    [Fact]
    public async Task Removing_a_line_hole_from_the_collection_deletes_the_row()
    {
        var testDb = new TestDb();
        using (var seed = testDb.CreateDbContext())
        {
            var line = new Line { Description = "Л-1", LineType = LineType.Канава };
            line.LineHoles.Add(new LineHole { BoreholeId = 5, SpreadAzimuth = 10 });
            seed.Lines.Add(line);
            seed.SaveChanges();
        }

        var service = new LinesService(testDb);
        var lines = await service.GetLinesAsync();
        var line2 = Assert.Single(lines);
        line2.LineHoles.Remove(line2.LineHoles.Single());   // как удаление строки в нижнем гриде
        await service.SaveAsync();

        using var db = testDb.CreateDbContext();
        Assert.Empty(db.LineHoles);
        Assert.Single(db.Lines);
    }

    [Fact]
    public async Task Duplicate_borehole_in_a_line_blocks_the_save()
    {
        var testDb = new TestDb();
        var service = new LinesService(testDb);
        var lines = await service.GetLinesAsync();
        var line = new Line { Description = "Л-1", LineType = LineType.Канава };
        line.LineHoles.Add(new LineHole { BoreholeId = 7, SpreadAzimuth = 10 });
        line.LineHoles.Add(new LineHole { BoreholeId = 7, SpreadAzimuth = 20 });
        lines.Add(line);

        var exception = await Assert.ThrowsAsync<DataValidationException>(() => service.SaveAsync());

        Assert.Contains(exception.Errors, e => e.EndsWith("Эта скважина уже есть в линии."));
        using var db = testDb.CreateDbContext();
        Assert.Empty(db.Lines);
    }

    [Fact]
    public async Task Seam_with_parts_and_coefficients_is_saved_and_duplicate_coefficients_are_blocked()
    {
        var testDb = new TestDb();
        var service = new SeamsService(testDb);
        var seams = await service.GetSeamsAsync();
        var seam = new Seam { Description = "Пласт 1", ShortName = "П1" };
        seam.Parts.Add(new Part { Npart = "1", N = 1 });
        seam.Coeffs.Add(new Coeff { InterbedId = 100, CoalType = 1, CoalGrade = 2 });
        seams.Add(seam);
        await service.SaveAsync();

        using (var db = testDb.CreateDbContext())
        {
            Assert.Single(db.Parts);
            Assert.Single(db.Coeffs);
        }

        // меняем у существующей строки прослой, тип и марку (ключ в модели приложения — CoeffId)
        var coeff = seam.Coeffs.Single();
        coeff.InterbedId = 200;
        coeff.CoalGrade = 9;
        await service.SaveAsync();
        using (var db = testDb.CreateDbContext())
            Assert.Equal(200, db.Coeffs.Single().InterbedId);

        seam.Coeffs.Add(new Coeff { InterbedId = 200, CoalType = 1, CoalGrade = 9 });
        await Assert.ThrowsAsync<DataValidationException>(() => service.SaveAsync());
    }
}
