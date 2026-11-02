namespace Geonicle.Infrastructure
{
    /// <summary>Разделяющий прослой для списка на ленте: код (N × 100) и подпись «Прослой …».</summary>
    public record InterbedItem(int InterbedId, string InterbedName);

    /// <summary>Разведочная линия для списка на ленте.</summary>
    public record LineItem(int LineId, decimal N, string Description)
    {
        /// <summary>Текст в закрытом поле списка.</summary>
        public string DisplayName => $"{N:0.##} — {Description}";
    }

    /// <summary>Скважина для списка на ленте: N — порядковый номер в линии (при «Все скважины» не задан).</summary>
    public record LineBoreholeItem(int BoreholeId, decimal? N, string HoleName)
    {
        /// <summary>Текст в закрытом поле списка.</summary>
        public string DisplayName => HoleName;
    }
}
