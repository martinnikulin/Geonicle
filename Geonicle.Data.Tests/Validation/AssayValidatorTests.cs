using Data.Models;
using Geonicle.Validation;
using Xunit;

namespace Geonicle.Data.Tests.Validation;

public class AssayValidatorTests
{
    private readonly AssayValidator validator = new();

    [Fact]
    public void Value_up_to_200_characters_is_valid()
    {
        Assert.True(validator.Validate(new Assay { Val = new string('1', 200) }).IsValid);
        Assert.True(validator.Validate(new Assay { Val = null }).IsValid);
    }

    [Fact]
    public void Value_longer_than_200_characters_is_rejected()
    {
        var result = validator.Validate(new Assay { Val = new string('1', 201) });
        Assert.True(result.HasMessage("Значение показателя не может быть длиннее 200 символов."));
    }

    [Fact]
    public void Describe_names_the_variable()
    {
        Assert.Equal("Анализ показателя 1001", validator.Describe(new Assay { VarId = 1001 }));
    }
}
