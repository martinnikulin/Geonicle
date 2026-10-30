namespace Geonicle.Infrastructure
{
    /// <summary>Скважина для выпадающих списков: только код и название, без отслеживания.</summary>
    public record BoreholeLookupItem(int BoreholeId, string HoleName);
}
