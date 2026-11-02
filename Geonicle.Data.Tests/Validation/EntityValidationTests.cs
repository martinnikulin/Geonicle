using Data.Models;
using Geonicle.Validation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Geonicle.Data.Tests.Validation;

public class EntityValidationTests
{
    private static Borehole Borehole(string name, decimal depth = 1) =>
        new() { HoleName = name, Depth = depth, HoleType = HoleType.Скважина };

    [Fact]
    public void Added_invalid_entities_are_reported_with_descriptions()
    {
        using var db = new TestDb().CreateDbContext();
        db.Boreholes.Add(Borehole("A-1", depth: -1));
        db.Boreholes.Add(Borehole(""));

        var errors = EntityValidation.Validate(db);

        Assert.Contains("Скважина «A-1»: Глубина скважины не может быть отрицательной.", errors);
        Assert.Contains("Скважина (без названия): Название скважины обязательно.", errors);
    }

    [Fact]
    public void Valid_entities_give_no_errors()
    {
        using var db = new TestDb().CreateDbContext();
        db.Boreholes.Add(Borehole("A-1"));
        Assert.Empty(EntityValidation.Validate(db));
    }

    [Fact]
    public void Modified_entity_is_validated_and_unchanged_is_not()
    {
        var testDb = new TestDb();
        using (var seed = testDb.CreateDbContext())
        {
            seed.Boreholes.AddRange(Borehole("Changed"), Borehole("Untouched"));
            seed.SaveChanges();
        }

        using var db = testDb.CreateDbContext();
        db.Boreholes.Single(b => b.HoleName == "Changed").Depth = -3;

        var errors = EntityValidation.Validate(db);

        Assert.Single(errors);
        Assert.StartsWith("Скважина «Changed»", errors[0]);
    }

    [Fact]
    public void Deleted_entities_are_not_validated()
    {
        var testDb = new TestDb();
        using (var seed = testDb.CreateDbContext())
        {
            seed.Boreholes.Add(Borehole("Bad", depth: -1));   // неверная строка уже в БД
            seed.SaveChanges();
        }

        using var db = testDb.CreateDbContext();
        db.Boreholes.Remove(db.Boreholes.Single());

        Assert.Empty(EntityValidation.Validate(db));
    }

    [Fact]
    public void Entities_without_validator_are_ignored()
    {
        using var db = new TestDb().CreateDbContext();
        db.ChangedRecords.Add(new ChangedRecord { TableName = "", TableId = -1 });
        Assert.Empty(EntityValidation.Validate(db));
    }

    [Fact]
    public void Identical_messages_are_reported_once()
    {
        using var db = new TestDb().CreateDbContext();
        var borehole = Borehole("A");
        var first = new Survey { Depth = 10 };
        var second = new Survey { Depth = 10 };
        borehole.Surveys.Add(first);
        borehole.Surveys.Add(second);
        db.Boreholes.Add(borehole);

        var errors = EntityValidation.Validate(db);

        Assert.Single(errors, e => e.Contains("уже есть у этой скважины"));
    }

    [Fact]
    public void User_message_lists_errors_and_truncates()
    {
        var errors = Enumerable.Range(1, 12).Select(i => $"Ошибка {i}").ToList();
        var message = new DataValidationException(errors).ToUserMessage(maxErrors: 10);

        Assert.StartsWith("Нельзя сохранить: есть ошибки в данных.", message);
        Assert.Equal(10, message.Split('\n').Count(line => line.StartsWith("•")));
        Assert.EndsWith("и ещё 2", message);
    }
}
