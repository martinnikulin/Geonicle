namespace Geonicle.Data.Services
{
    /// <summary>Допустимые значения поля «КБ» у пластопересечения (в БД — строка до 12 символов).</summary>
    public static class HoleSeamKb
    {
        public static readonly IReadOnlyList<string> Values =
            new[] { "Б", "Б(без керна)", "К", "К+Б", "Б(К)", "Б(НК)", "Нет данных" };
    }
}
