using Data.Models;
using Geonicle.Validation;
using Xunit;

namespace Geonicle.Data.Tests.Validation;

public class AverageValidatorTests
{
    private readonly AverageValidator validator = new();

    [Fact]
    public void Average_with_a_variable_is_valid_even_without_type_and_grade()
    {
        Assert.True(validator.Validate(new Average { VarId = 10, CoalType = 0, CoalGrade = 0 }).IsValid);
    }

    [Fact]
    public void Variable_is_required()
    {
        Assert.True(validator.Validate(new Average { VarId = 0 }).HasMessage("Выберите показатель."));
    }

    [Fact]
    public void Describe_uses_the_seam_name()
    {
        var average = new Average { VarId = 1 };
        Assert.Equal("Среднее", validator.Describe(average));
        average.Seam = new Seam { Description = "Пласт 1" };
        Assert.Equal("Среднее по пласту «Пласт 1»", validator.Describe(average));
    }
}
