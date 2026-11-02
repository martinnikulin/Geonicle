using Data.Models;
using Geonicle.Validation;
using Xunit;

namespace Geonicle.Data.Tests.Validation;

public class SeamValidatorTests
{
    private readonly SeamValidator validator = new();

    private static Seam Seam() => new() { Description = "Пласт 1", ShortName = "П1", AdRoof = 10, AdBed = 10, DdaRoof = 10, DdaBed = 10 };

    [Fact]
    public void Valid_seam_passes() => Assert.True(validator.Validate(Seam()).IsValid);

    [Fact]
    public void Nullable_N_and_strata_may_be_empty()
    {
        var seam = Seam();
        seam.N = null;
        seam.Strata = null;
        Assert.True(validator.Validate(seam).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Name_and_short_name_are_required(string value)
    {
        var seam = Seam();
        seam.Description = value;
        seam.ShortName = value;
        var result = validator.Validate(seam);
        Assert.True(result.HasMessage("Название пласта обязательно."));
        Assert.True(result.HasMessage("Короткое название пласта обязательно."));
    }

    [Fact]
    public void Length_limits_are_checked()
    {
        var seam = Seam();
        seam.Description = new string('x', 51);
        seam.ShortName = new string('x', 51);
        seam.GroupSeamId = new string('g', 13);
        var result = validator.Validate(seam);
        Assert.True(result.HasMessage("Название пласта не может быть длиннее 50 символов."));
        Assert.True(result.HasMessage("Короткое название пласта не может быть длиннее 50 символов."));
        Assert.True(result.HasMessage("Код группы пласта не может быть длиннее 12 символов."));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(100, true)]
    [InlineData(-0.1, false)]
    [InlineData(100.1, false)]
    public void Ad_and_Dda_must_be_between_0_and_100(double value, bool valid)
    {
        var v = (decimal)value;
        foreach (var setter in new Action<Seam>[]
                 {
                     s => s.AdRoof = v, s => s.AdBed = v, s => s.DdaRoof = v, s => s.DdaBed = v
                 })
        {
            var seam = Seam();
            setter(seam);
            Assert.Equal(valid, validator.Validate(seam).IsValid);
        }
    }

    [Fact]
    public void Negative_N_and_strata_are_rejected()
    {
        var seam = Seam();
        seam.N = -1;
        seam.Strata = -2;
        var result = validator.Validate(seam);
        Assert.True(result.HasMessage("N пласта не может быть отрицательным."));
        Assert.True(result.HasMessage("Страта не может быть отрицательной."));
    }

    [Fact]
    public void Describe_uses_name_or_placeholder()
    {
        Assert.Equal("Пласт «Пласт 1»", validator.Describe(Seam()));
        Assert.Equal("Пласт (без названия)", validator.Describe(new Seam()));
    }
}
