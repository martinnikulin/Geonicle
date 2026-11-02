using Data.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Geonicle.Data.Tests.Integration;

/// <summary>Ограничения схемы, которые InMemory не проверяет. Работают через Db.SaveChanges, без проверки валидаторами.</summary>
[Collection("SqlServer")]
public class ConstraintTests : SqlServerTestBase
{
    [SqlServerFact]
    public void Survey_depth_is_unique_within_a_borehole()
    {
        var borehole = NewBorehole();
        borehole.Surveys.Add(new Survey { Depth = 5 });
        Db.Boreholes.Add(borehole);
        Db.SaveChanges();

        borehole.Surveys.Add(new Survey { Depth = 5 });

        Assert.Throws<DbUpdateException>(() => Db.SaveChanges());
    }

    [SqlServerFact]
    public void Line_hole_requires_an_existing_borehole()
    {
        var line = new Line { Description = Unique("it-line"), LineType = LineType.Канава };
        line.LineHoles.Add(new LineHole { BoreholeId = int.MaxValue });
        Db.Lines.Add(line);

        Assert.Throws<DbUpdateException>(() => Db.SaveChanges());
    }

    [SqlServerFact]
    public void Part_requires_an_existing_seam()
    {
        Db.Parts.Add(new Part { SeamId = int.MaxValue, Npart = "1", Description = "", N = 1 });

        Assert.Throws<DbUpdateException>(() => Db.SaveChanges());
    }

    [SqlServerFact]
    public void Borehole_name_longer_than_50_is_rejected()
    {
        var borehole = NewBorehole();
        borehole.HoleName = new string('x', 51);
        Db.Boreholes.Add(borehole);

        Assert.Throws<DbUpdateException>(() => Db.SaveChanges());
    }

    [SqlServerFact]
    public void Coefficient_combination_is_unique_within_a_seam()
    {
        var seam = NewSeam();
        seam.Coeffs.Add(new Coeff { InterbedId = 100, CoalType = 1, CoalGrade = 2 });
        seam.Coeffs.Add(new Coeff { InterbedId = 100, CoalType = 1, CoalGrade = 2 });
        Db.Seams.Add(seam);

        Assert.Throws<DbUpdateException>(() => Db.SaveChanges());
    }
}
