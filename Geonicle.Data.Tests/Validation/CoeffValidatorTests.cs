using Data.Models;
using Geonicle.Validation;
using Xunit;

namespace Geonicle.Data.Tests.Validation;

public class CoeffValidatorTests
{
    private readonly CoeffValidator validator = new();

    private static Coeff Coeff(int interbed, int type, int grade) =>
        new() { InterbedId = interbed, CoalType = type, CoalGrade = grade };

    [Fact]
    public void Zero_interbed_type_and_grade_are_allowed()
    {
        var seam = new Seam { Description = "П" };
        var coeff = Coeff(0, 0, 0);
        seam.Coeffs.Add(coeff);
        coeff.Seam = seam;
        Assert.True(validator.Validate(coeff).IsValid);
    }

    [Fact]
    public void Same_combination_in_one_seam_is_a_duplicate()
    {
        var seam = new Seam { Description = "П" };
        var first = Coeff(100, 1, 2);
        var second = Coeff(100, 1, 2);
        seam.Coeffs.Add(first);
        seam.Coeffs.Add(second);
        first.Seam = seam;
        second.Seam = seam;

        Assert.True(validator.Validate(first).HasMessage("Коэффициент с таким прослоем, типом и маркой уже есть у этого пласта."));
    }

    [Fact]
    public void Two_zero_rows_are_also_a_duplicate()
    {
        var seam = new Seam { Description = "П" };
        var first = Coeff(0, 0, 0);
        seam.Coeffs.Add(first);
        seam.Coeffs.Add(Coeff(0, 0, 0));
        first.Seam = seam;

        Assert.False(validator.Validate(first).IsValid);
    }

    [Fact]
    public void Different_combinations_are_not_duplicates()
    {
        var seam = new Seam { Description = "П" };
        var first = Coeff(100, 1, 2);
        seam.Coeffs.Add(first);
        seam.Coeffs.Add(Coeff(100, 1, 3));
        first.Seam = seam;

        Assert.True(validator.Validate(first).IsValid);
    }

    [Fact]
    public void Describe_includes_seam_name()
    {
        var coeff = Coeff(0, 0, 0);
        Assert.Equal("Коэффициент", validator.Describe(coeff));
        coeff.Seam = new Seam { Description = "Пласт 1" };
        Assert.Equal("Коэффициент пласта «Пласт 1»", validator.Describe(coeff));
    }
}
