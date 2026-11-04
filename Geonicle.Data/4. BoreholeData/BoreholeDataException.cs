namespace Geonicle.Data.Services
{
    /// <summary>Действие с данными скважины невозможно; текст предназначен для показа пользователю.</summary>
    public class BoreholeDataException : Exception
    {
        public BoreholeDataException(string message)
            : base(message)
        {
        }
    }
}
