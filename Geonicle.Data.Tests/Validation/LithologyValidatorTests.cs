using Data.Models;
using Geonicle.Validation;
using Xunit;

namespace Geonicle.Data.Tests.Validation;

public class LithologyValidatorTests
{
    private readonly LithologyValidator validator = new();

    private static Lithology Layer(decimal from, decimal to) => new() { DepthFrom = from, DepthTo = to };

    private static Borehole HoleWith(params Lithology[] layers)
    {
        var hole = new Borehole { HoleName = "A-1" };
        foreach (var layer in layers)
        {
            hole.Lithologies.Add(layer);
            layer.Borehole = hole;
        }
        return hole;
    }

    [Fact]
    public void Valid_layer_passes()
    {
        var layer = Layer(0, 5);
        layer.Core = 4; layer.CoreNorm = 4; layer.CoreProc = 80; layer.Th = 5; layer.ThNorm = 5;
        Assert.True(validator.Validate(layer).IsValid);
    }

    [Fact]
    public void Negative_depth_from_is_rejected()
    {
        Assert.True(validator.Validate(Layer(-1, 5)).HasMessage("Глубина «От» не может быть отрицательной."));
    }

    [Fact]
    public void Depth_to_must_be_greater_than_depth_from()
    {
        var message = "Глубина «До» должна быть больше «От».";
        Assert.True(validator.Validate(Layer(5, 5)).HasMessage(message));
        Assert.True(validator.Validate(Layer(5, 4)).HasMessage(message));
        Assert.False(validator.Validate(Layer(5, 6)).HasMessage(message));
    }

    [Fact]
    public void Negative_thickness_is_rejected()
    {
        var layer = Layer(0, 5);
        layer.Th = -1;
        layer.ThNorm = -1;
        var result = validator.Validate(layer);
        Assert.True(result.HasMessage("Мощность не может быть отрицательной."));
        Assert.True(result.HasMessage("Мощность (норм) не может быть отрицательной."));
    }

    [Fact]
    public void Core_values_may_be_empty_but_not_negative()
    {
        var layer = Layer(0, 5);
        Assert.True(validator.Validate(layer).IsValid);
        layer.Core = -1;
        layer.CoreNorm = -1;
        var result = validator.Validate(layer);
        Assert.True(result.HasMessage("Керн не может быть отрицательным."));
        Assert.True(result.HasMessage("Керн (норм) не может быть отрицательным."));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Core_percent_outside_0_100_is_rejected(int percent)
    {
        var layer = Layer(0, 5);
        layer.CoreProc = percent;
        Assert.True(validator.Validate(layer).HasMessage("Керн, % должен быть от 0 до 100."));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Core_percent_bounds_are_allowed(int percent)
    {
        var layer = Layer(0, 5);
        layer.CoreProc = percent;
        Assert.True(validator.Validate(layer).IsValid);
    }

    [Fact]
    public void Same_depth_from_in_one_borehole_is_a_duplicate()
    {
        var first = Layer(0, 5);
        HoleWith(first, Layer(0, 7));
        Assert.True(validator.Validate(first).HasMessage("Слой с такой глубиной «От» уже есть в этой скважине."));
    }

    [Fact]
    public void Same_depth_to_in_one_borehole_is_a_duplicate()
    {
        var first = Layer(0, 5);
        HoleWith(first, Layer(2, 5));
        Assert.True(validator.Validate(first).HasMessage("Слой с такой глубиной «До» уже есть в этой скважине."));
    }

    [Fact]
    public void Different_depths_are_not_duplicates()
    {
        var first = Layer(0, 5);
        HoleWith(first, Layer(5, 10));
        Assert.True(validator.Validate(first).IsValid);
    }

    [Fact]
    public void Describe_shows_the_interval()
    {
        Assert.Equal("Литология 1–2", validator.Describe(Layer(1, 2)));
    }
}
