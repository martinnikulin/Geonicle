using Data.Models;
using Geonicle.Validation;
using Xunit;

namespace Geonicle.Data.Tests.Validation;

public class PartValidatorTests
{
    private readonly PartValidator validator = new();

    [Fact]
    public void Empty_number_and_description_are_allowed() =>
        Assert.True(validator.Validate(new Part { Npart = "", Description = null, N = 0 }).IsValid);

    [Fact]
    public void Length_limits_are_checked()
    {
        var result = validator.Validate(new Part
        {
            Chapter = new string('c', 51), Description = new string('d', 151), Npart = new string('p', 13)
        });
        Assert.True(result.HasMessage("Приложение не может быть длиннее 50 символов."));
        Assert.True(result.HasMessage("Описание детали не может быть длиннее 150 символов."));
        Assert.True(result.HasMessage("Номер детали не может быть длиннее 12 символов."));
    }

    [Fact]
    public void Limits_themselves_pass() =>
        Assert.True(validator.Validate(new Part
        {
            Chapter = new string('c', 50), Description = new string('d', 150), Npart = new string('p', 12)
        }).IsValid);

    [Fact]
    public void Negative_N_is_rejected() =>
        Assert.True(validator.Validate(new Part { N = -1 }).HasMessage("N детали не может быть отрицательным."));

    [Fact]
    public void Describe_includes_number_and_seam()
    {
        var part = new Part { Npart = "2" };
        Assert.Equal("Деталь «2»", validator.Describe(part));
        part.Seam = new Seam { Description = "Пласт 1" };
        Assert.Equal("Деталь «2» пласта «Пласт 1»", validator.Describe(part));
        Assert.Equal("Деталь (без номера)", validator.Describe(new Part()));
    }
}
