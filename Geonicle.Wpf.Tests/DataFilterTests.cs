using Geonicle.Wpf.Ribbon;
using Xunit;

namespace Geonicle.Wpf.Tests;

public class DataFilterTests
{
    [Fact]
    public void Defaults_mean_no_filter()
    {
        var filter = new DataFilter();

        Assert.Null(filter.InterbedId);
        Assert.Null(filter.SeamId);
        Assert.Null(filter.LineId);
        Assert.Null(filter.BoreholeId);
        Assert.False(filter.AllBoreholes);
    }

    [Fact]
    public void Every_change_raises_Changed_once()
    {
        var filter = new DataFilter();
        var raised = 0;
        filter.Changed += (_, _) => raised++;

        filter.InterbedId = 100;
        filter.LineId = 10;
        filter.BoreholeId = 1;
        filter.AllBoreholes = true;
        filter.SeamId = 5;

        Assert.Equal(5, raised);
        Assert.Equal((100, 10, 1, true), (filter.InterbedId, filter.LineId, filter.BoreholeId, filter.AllBoreholes));
    }

    [Fact]
    public void Setting_the_same_value_does_not_raise_Changed()
    {
        var filter = new DataFilter { InterbedId = 100 };
        var raised = 0;
        filter.Changed += (_, _) => raised++;

        filter.InterbedId = 100;

        Assert.Equal(0, raised);
    }
}
