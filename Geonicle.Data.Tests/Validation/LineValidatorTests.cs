using Data.Models;
using Geonicle.Validation;
using Xunit;

namespace Geonicle.Data.Tests.Validation;

public class LineValidatorTests
{
    private readonly LineValidator validator = new();

    private static Line Line(string name = "Л-1") => new() { Description = name, LineType = LineType.Канава, N = 1 };

    [Fact]
    public void Valid_line_passes() => Assert.True(validator.Validate(Line()).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Name_is_required(string name) =>
        Assert.True(validator.Validate(Line(name)).HasMessage("Название линии обязательно."));

    [Fact]
    public void Name_longer_than_50_is_rejected() =>
        Assert.True(validator.Validate(Line(new string('x', 51))).HasMessage("Название линии не может быть длиннее 50 символов."));

    [Fact]
    public void Unknown_line_type_is_rejected()
    {
        var line = Line();
        line.LineType = (LineType)999;
        Assert.True(validator.Validate(line).HasMessage("Выберите тип линии."));
    }

    [Fact]
    public void Negative_N_is_rejected()
    {
        var line = Line();
        line.N = -1;
        Assert.True(validator.Validate(line).HasMessage("N линии не может быть отрицательным."));
    }

    [Fact]
    public void Describe_uses_name_or_placeholder()
    {
        Assert.Equal("Линия «Л-1»", validator.Describe(Line()));
        Assert.Equal("Линия (без названия)", validator.Describe(Line("")));
    }
}
