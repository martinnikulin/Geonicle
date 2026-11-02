using System.Text;

namespace Geonicle.Validation
{
    /// <summary>Данные не прошли проверку перед сохранением; в БД ничего не записано.</summary>
    public class DataValidationException : Exception
    {
        public DataValidationException(IReadOnlyList<string> errors)
            : base("Нельзя сохранить: есть ошибки в данных.")
        {
            Errors = errors;
        }

        public IReadOnlyList<string> Errors { get; }

        /// <summary>Текст для диалога: заголовок и список ошибок (не больше <paramref name="maxErrors"/>, остальные счётчиком).</summary>
        public string ToUserMessage(int maxErrors = 10)
        {
            var text = new StringBuilder(Message).AppendLine().AppendLine();
            foreach (var error in Errors.Take(maxErrors))
                text.Append("• ").AppendLine(error);
            if (Errors.Count > maxErrors)
                text.Append("и ещё ").Append(Errors.Count - maxErrors);
            return text.ToString().TrimEnd();
        }
    }
}
