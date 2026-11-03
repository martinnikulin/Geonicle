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

    private static Lithology Layer(int id, decimal from, decimal to) => new() { LithologyId = id, DepthFrom = from, DepthTo = to };

    /// <summary>Пластопересечение между слоями; все пластопересечения скважины попадают в её коллекцию.</summary>
    private static SeamDef InHole(Borehole hole, int interbed, Lithology from, Lithology to)
    {
        var seam = new SeamDef
        {
            InterbedId = interbed,
            DepthFromId = from.LithologyId,
            DepthFromNavigation = from,
            DepthToId = to.LithologyId,
            DepthToNavigation = to,
            Borehole = hole,
        };
        hole.SeamDefs.Add(seam);
        return seam;
    }

    [Fact]
    public void Lower_boundary_must_be_deeper_than_the_upper()
    {
        var hole = new Borehole { HoleName = "A" };
        var seam = InHole(hole, 100, Layer(2, 10, 20), Layer(1, 0, 5));   // «До» выше «От»
        Assert.True(validator.Validate(seam).HasMessage("Нижняя граница пластопересечения должна быть глубже верхней."));
    }

    [Fact]
    public void Seam_between_two_different_layers_is_valid()
    {
        var hole = new Borehole { HoleName = "A" };
        var seam = InHole(hole, 100, Layer(1, 0, 5), Layer(2, 5, 10));
        Assert.True(validator.Validate(seam).IsValid, validator.Validate(seam).Messages());
    }

    [Fact]
    public void Same_upper_or_lower_boundary_in_one_interbed_is_a_duplicate()
    {
        var hole = new Borehole { HoleName = "A" };
        var l1 = Layer(1, 0, 5);
        var l2 = Layer(2, 5, 10);
        var l3 = Layer(3, 10, 15);
        var first = InHole(hole, 100, l1, l2);
        InHole(hole, 100, l1, l3);   // та же верхняя граница
        InHole(hole, 100, l3, l2);   // та же нижняя граница

        var result = validator.Validate(first);
        Assert.True(result.HasMessage("В этом прослое уже есть пластопересечение с такой верхней границей."));
        Assert.True(result.HasMessage("В этом прослое уже есть пластопересечение с такой нижней границей."));
    }

    [Fact]
    public void Same_boundaries_in_another_interbed_are_allowed()
    {
        var hole = new Borehole { HoleName = "A" };
        var l1 = Layer(1, 0, 5);
        var l2 = Layer(2, 5, 10);
        var first = InHole(hole, 100, l1, l2);
        InHole(hole, 200, l1, l2);
        Assert.True(validator.Validate(first).IsValid, validator.Validate(first).Messages());
    }

    [Fact]
    public void Overlapping_seams_of_one_interbed_are_rejected()
    {
        var hole = new Borehole { HoleName = "A" };
        var first = InHole(hole, 100, Layer(1, 0, 5), Layer(3, 15, 20));
        InHole(hole, 100, Layer(2, 10, 12), Layer(4, 25, 30));   // 10–30 пересекается с 0–20

        Assert.True(validator.Validate(first).HasMessage("Пластопересечение пересекается с другим пластопересечением этого прослоя."));
    }

    [Fact]
    public void Adjacent_seams_do_not_overlap()
    {
        var hole = new Borehole { HoleName = "A" };
        var first = InHole(hole, 100, Layer(1, 0, 5), Layer(2, 5, 10));
        InHole(hole, 100, Layer(3, 10, 15), Layer(4, 15, 20));   // 10–20 после 0–10
        Assert.True(validator.Validate(first).IsValid, validator.Validate(first).Messages());
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
