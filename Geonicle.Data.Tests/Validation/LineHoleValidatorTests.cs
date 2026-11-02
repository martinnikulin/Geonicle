using Data.Models;
using Geonicle.Validation;
using Xunit;

namespace Geonicle.Data.Tests.Validation;

public class LineHoleValidatorTests
{
    private readonly LineHoleValidator validator = new();

    private static LineHole LineHole(int boreholeId = 5, int lineId = 1, decimal azimuth = 90) =>
        new() { BoreholeId = boreholeId, LineId = lineId, SpreadAzimuth = azimuth };

    [Fact]
    public void Valid_line_hole_passes() => Assert.True(validator.Validate(LineHole()).IsValid);

    [Fact]
    public void Borehole_must_be_chosen() =>
        Assert.True(validator.Validate(LineHole(boreholeId: 0)).HasMessage("Выберите скважину."));

    [Fact]
    public void Row_without_line_key_and_without_line_is_rejected() =>
        Assert.True(validator.Validate(LineHole(lineId: 0)).HasMessage("Строка должна принадлежать линии."));

    [Fact]
    public void Row_without_line_key_but_with_line_passes()
    {
        var row = LineHole(lineId: 0);
        row.Line = new Line { Description = "Л-1" };
        Assert.True(validator.Validate(row).IsValid);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(360, true)]
    [InlineData(-1, false)]
    [InlineData(361, false)]
    public void Azimuth_must_be_between_0_and_360(double azimuth, bool valid) =>
        Assert.Equal(valid, validator.Validate(LineHole(azimuth: (decimal)azimuth)).IsValid);

    [Fact]
    public void Same_borehole_twice_in_a_line_is_a_duplicate()
    {
        var line = new Line { Description = "Л-1" };
        var first = LineHole(boreholeId: 7);
        var second = LineHole(boreholeId: 7);
        line.LineHoles.Add(first);
        line.LineHoles.Add(second);
        first.Line = line;
        second.Line = line;

        Assert.True(validator.Validate(first).HasMessage("Эта скважина уже есть в линии."));
    }

    [Fact]
    public void Several_unchosen_boreholes_are_not_reported_as_duplicates()
    {
        var line = new Line { Description = "Л-1" };
        var first = LineHole(boreholeId: 0);
        line.LineHoles.Add(first);
        line.LineHoles.Add(LineHole(boreholeId: 0));
        first.Line = line;

        Assert.False(validator.Validate(first).HasMessage("Эта скважина уже есть в линии."));
    }

    [Fact]
    public void Describe_includes_line_name()
    {
        var row = LineHole();
        Assert.Equal("Скважина в линии", validator.Describe(row));
        row.Line = new Line { Description = "Л-1" };
        Assert.Equal("Скважина в линии «Л-1»", validator.Describe(row));
    }
}
