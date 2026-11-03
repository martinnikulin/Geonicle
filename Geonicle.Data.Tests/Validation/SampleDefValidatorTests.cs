using Data.Models;
using Geonicle.Validation;
using Xunit;

namespace Geonicle.Data.Tests.Validation;

public class SampleDefValidatorTests
{
    private readonly SampleDefValidator validator = new();

    private static SampleDef Valid() => new() { DepthFrom = 10, DepthTo = 12, Nsample = "4117 сн.1" };

    [Fact]
    public void Valid_sample_passes() => Assert.True(validator.Validate(Valid()).IsValid);

    [Fact]
    public void Negative_depth_from_is_rejected()
    {
        var sample = Valid();
        sample.DepthFrom = -1;
        Assert.True(validator.Validate(sample).HasMessage("Глубина «От» не может быть отрицательной."));
    }

    [Fact]
    public void Depth_to_must_be_greater_than_depth_from()
    {
        var message = "Глубина «До» должна быть больше «От».";
        var sample = Valid();
        sample.DepthTo = 10;
        Assert.True(validator.Validate(sample).HasMessage(message));
        sample.DepthTo = 9;
        Assert.True(validator.Validate(sample).HasMessage(message));
    }

    [Fact]
    public void Sample_number_is_optional_and_limited_to_200()
    {
        var sample = Valid();
        sample.Nsample = null;
        Assert.True(validator.Validate(sample).IsValid);
        sample.Nsample = new string('x', 201);
        Assert.True(validator.Validate(sample).HasMessage("Номер пробы не может быть длиннее 200 символов."));
        sample.Nsample = new string('x', 200);
        Assert.True(validator.Validate(sample).IsValid);
    }

    [Fact]
    public void Describe_uses_the_sample_number()
    {
        var sample = Valid();
        Assert.Equal("Проба «4117 сн.1»", validator.Describe(sample));
        sample.Nsample = " ";
        Assert.Equal("Проба", validator.Describe(sample));
    }
}
