using Data.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Geonicle.Data.Tests.Integration;

/// <summary>Поведение, которое остаётся за БД: ключи identity, метки времени и триггеры, которые не переносились в код.</summary>
[Collection("SqlServer")]
public class DatabaseBehaviorTests : SqlServerTestBase
{
    [SqlServerFact]
    public void Database_generates_keys_for_new_rows()
    {
        var borehole = NewBorehole();
        var seam = NewSeam();
        var line = new Line { Description = Unique("it-line"), LineType = LineType.Канава };
        Db.AddRange(borehole, seam, line);

        Db.SaveChanges();

        Assert.True(borehole.BoreholeId > 0);
        Assert.True(seam.SeamId > 0);
        Assert.True(line.LineId > 0);
    }

    [SqlServerFact]
    public void Creation_and_modification_times_are_set_by_the_server()
    {
        var borehole = NewBorehole();
        Db.Boreholes.Add(borehole);
        Db.SaveChanges();
        Assert.True(borehole.DateCreated.Year > 2000);

        borehole.Depth = 99;
        Db.SaveChanges();
        Db.Entry(borehole).Reload();

        Assert.True(borehole.DateModified >= borehole.DateCreated);
    }

    [SqlServerFact]
    public void Inserting_a_borehole_records_a_changed_survey_in_ChangedRecords()
    {
        // Остаётся в триггере AfterInsertHoles (в код не переносилось).
        var borehole = NewBorehole();
        Db.Boreholes.Add(borehole);
        Db.SaveChanges();

        Assert.True(Db.ChangedRecords.Any(c => c.TableName == "Survey" && c.TableId == borehole.BoreholeId));
    }

    [SqlServerFact]
    public void Deleting_a_borehole_with_its_loaded_children_works()
    {
        var borehole = NewBorehole();
        borehole.Surveys.Add(new Survey { Depth = 5 });
        Db.Boreholes.Add(borehole);
        Db.SaveChanges();

        // Загружаем зависимые строки (и те, что мог создать триггер БД), чтобы EF удалил их первыми.
        var loaded = Db.Boreholes
            .Include(b => b.Surveys)
            .Include(b => b.LineHoles)
            .Single(b => b.BoreholeId == borehole.BoreholeId);
        Db.Boreholes.Remove(loaded);

        Db.SaveChanges();

        Assert.False(Db.Boreholes.Any(b => b.BoreholeId == borehole.BoreholeId));
    }
}
