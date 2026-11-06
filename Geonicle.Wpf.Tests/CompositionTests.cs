using Geonicle.Data.Services;
using Geonicle.Infrastructure;
using Geonicle.Wpf.Ribbon;
using Geonicle.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Geonicle.Wpf.Tests;

/// <summary>Сборка контейнера приложения: все экраны и сервисы создаются (против ошибок вроде «Multiple constructors»).</summary>
public class CompositionTests
{
    private static ServiceProvider Build() =>
        new ServiceCollection()
            .AddGeonicleApp("Server=localhost;Database=GDB520001;Integrated Security=True;TrustServerCertificate=True")
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

    [Theory]
    [InlineData(typeof(MainViewModel))]
    [InlineData(typeof(RibbonFilterViewModel))]
    [InlineData(typeof(DataFilter))]
    [InlineData(typeof(IFilterLookupService))]
    [InlineData(typeof(BoreholesViewModel))]
    [InlineData(typeof(LinesViewModel))]
    [InlineData(typeof(SeamsViewModel))]
    [InlineData(typeof(BoreholeDataViewModel))]
    [InlineData(typeof(SeamIntersectionsViewModel))]
    [InlineData(typeof(ReserveBlocksViewModel))]
    [InlineData(typeof(IReserveBlocksService))]
    [InlineData(typeof(ISeamIntersectionsService))]
    [InlineData(typeof(AssaysViewModel))]
    [InlineData(typeof(IAssayService))]
    [InlineData(typeof(IAssaysWindowService))]
    [InlineData(typeof(IBoreholesService))]
    [InlineData(typeof(ILinesService))]
    [InlineData(typeof(ISeamsService))]
    public void Registered_type_can_be_created(Type type)
    {
        using var provider = Build();

        Assert.NotNull(provider.GetRequiredService(type));
    }
}
