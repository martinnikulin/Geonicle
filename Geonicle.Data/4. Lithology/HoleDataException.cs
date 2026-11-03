namespace Geonicle.Data.Services
{
    /// <summary>Действие с данными скважины невозможно; текст предназначен для показа пользователю.</summary>
    public class HoleDataException : Exception
    {
        public HoleDataException(string message)
            : base(message)
        {
        }
    }
}
