using Data.Models;
using Geonicle.Validation;
using Xunit;

namespace Geonicle.Data.Tests.Validation;

public class SeamDefValidatorTests
{
    private readonly SeamDefValidator validator = new();

    private static SeamDef Valid() => new() { DrawFrom = 10, DrawTo = 12, Kb = "К", Comments = "низкий вых." };

    [Fact]
    public void Valid_seam_passes()
    {
        Assert.True(validator.Validate(Valid()).IsValid);
    }

    [Fact]
    public void Empty_kb_is_allowed()
    {
        var seam = Valid();
        seam.Kb = null;
        Assert.True(validator.Validate(seam).IsValid);
        seam.Kb = "";
        Assert.True(validator.Validate(seam).IsValid);
    }

    [Theory]
    [InlineData("Б")]
    [InlineData("Б(без керна)")]
    [InlineData("К")]
    [InlineData("К+Б")]
    [InlineData("Б(К)")]
    [InlineData("Б(НК)")]
    [InlineData("Нет данных")]
    public void Every_listed_kb_value_is_allowed(string kb)
    {
        var seam = Valid();
        seam.Kb = kb;
        Assert.True(validator.Validate(seam).IsValid);
    }

    [Fact]
    public void Unknown_kb_is_rejected()
    {
        var seam = Valid();
        seam.Kb = "X";
        Assert.True(validator.Validate(seam).HasMessage("КБ должен быть одним из значений списка."));
    }

    [Fact]
    public void Draw_to_less_than_draw_from_is_rejected()
    {
        var seam = Valid();
        seam.DrawTo = 9;
        Assert.True(validator.Validate(seam).HasMessage("«Стопки до» не может быть меньше «Стопки от»."));
    }

    [Fact]
    public void Equal_draw_values_are_allowed()
    {
        var seam = Valid();
        seam.DrawTo = seam.DrawFrom;
        Assert.True(validator.Validate(seam).IsValid);
    }

    [Fact]
    public void Comment_longer_than_20_is_rejected()
    {
        var seam = Valid();
        seam.Comments = new string('x', 21);
        Assert.True(validator.Validate(seam).HasMessage("Комментарий не может быть длиннее 20 символов."));
        seam.Comments = new string('x', 20);
        Assert.True(validator.Validate(seam).IsValid);
    }

    [Fact]
    public void Describe_uses_seam_name()
    {
        var seam = Valid();
        Assert.Equal("Пластопересечение", validator.Describe(seam));
        seam.Seam = new Seam { Description = "I(1)" };
        Assert.Equal("Пластопересечение «I(1)»", validator.Describe(seam));
    }
}
