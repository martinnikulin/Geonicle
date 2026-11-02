using Data.Models;
using Geonicle.Validation;
using Xunit;

namespace Geonicle.Data.Tests.Validation;

public class SurveyValidatorTests
{
    private readonly SurveyValidator validator = new();

    private static Survey Survey(decimal depth = 10, decimal dip = 45, decimal azimuth = 180) =>
        new() { Depth = depth, Dip = dip, Azimuth = azimuth };

    [Fact]
    public void Valid_survey_passes() => Assert.True(validator.Validate(Survey()).IsValid);

    [Theory]
    [InlineData(0, true)]
    [InlineData(-0.01, false)]
    public void Depth_must_not_be_negative(double depth, bool valid) =>
        Assert.Equal(valid, validator.Validate(Survey(depth: (decimal)depth)).IsValid);

    [Theory]
    [InlineData(0, true)]
    [InlineData(90, true)]
    [InlineData(-1, false)]
    [InlineData(90.5, false)]
    public void Dip_must_be_between_0_and_90(double dip, bool valid) =>
        Assert.Equal(valid, validator.Validate(Survey(dip: (decimal)dip)).IsValid);

    [Theory]
    [InlineData(0, true)]
    [InlineData(360, true)]
    [InlineData(-1, false)]
    [InlineData(360.5, false)]
    public void Azimuth_must_be_between_0_and_360(double azimuth, bool valid) =>
        Assert.Equal(valid, validator.Validate(Survey(azimuth: (decimal)azimuth)).IsValid);

    [Fact]
    public void Same_depth_in_one_borehole_is_a_duplicate()
    {
        var borehole = new Borehole { HoleName = "A" };
        var first = Survey(depth: 10);
        var second = Survey(depth: 10);
        borehole.Surveys.Add(first);
        borehole.Surveys.Add(second);
        first.Borehole = borehole;
        second.Borehole = borehole;

        Assert.True(validator.Validate(first).HasMessage("Замер с такой глубиной уже есть у этой скважины."));
    }

    [Fact]
    public void Different_depths_are_not_duplicates()
    {
        var borehole = new Borehole { HoleName = "A" };
        var first = Survey(depth: 10);
        borehole.Surveys.Add(first);
        borehole.Surveys.Add(Survey(depth: 20));
        first.Borehole = borehole;

        Assert.True(validator.Validate(first).IsValid);
    }

    [Fact]
    public void Describe_includes_borehole_name_and_depth()
    {
        var survey = Survey(depth: 12.5m);
        Assert.Equal("Замер на глубине 12,5", validator.Describe(survey).Replace('.', ','));
        survey.Borehole = new Borehole { HoleName = "A-1" };
        Assert.Equal("Замер скважины «A-1», глубина 12,5", validator.Describe(survey).Replace('.', ','));
    }
}
