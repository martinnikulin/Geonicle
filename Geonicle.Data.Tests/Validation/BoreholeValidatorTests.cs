using Data.Models;
using Geonicle.Validation;
using Xunit;

namespace Geonicle.Data.Tests.Validation;

public class BoreholeValidatorTests
{
    private readonly BoreholeValidator validator = new();

    [Fact]
    public void Valid_borehole_passes() =>
        Assert.True(validator.Validate(new Borehole { HoleName = "A-1", Depth = 100 }).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Name_is_required(string name)
    {
        var result = validator.Validate(new Borehole { HoleName = name, Depth = 1 });
        Assert.True(result.HasMessage("Название скважины обязательно."), result.Messages());
    }

    [Fact]
    public void Name_longer_than_50_is_rejected() =>
        Assert.True(validator.Validate(new Borehole { HoleName = new string('x', 51) })
            .HasMessage("Название скважины не может быть длиннее 50 символов."));

    [Fact]
    public void Name_of_50_and_zero_depth_pass() =>
        Assert.True(validator.Validate(new Borehole { HoleName = new string('x', 50), Depth = 0 }).IsValid);

    [Fact]
    public void Negative_depth_is_rejected() =>
        Assert.True(validator.Validate(new Borehole { HoleName = "A", Depth = -1 })
            .HasMessage("Глубина скважины не может быть отрицательной."));

    [Fact]
    public void All_errors_are_reported_together() =>
        Assert.Equal(2, validator.Validate(new Borehole { HoleName = "", Depth = -5 }).Errors.Count);

    [Fact]
    public void Describe_uses_name_or_placeholder()
    {
        Assert.Equal("Скважина «A-1»", validator.Describe(new Borehole { HoleName = "A-1" }));
        Assert.Equal("Скважина (без названия)", validator.Describe(new Borehole { HoleName = "" }));
    }
}
