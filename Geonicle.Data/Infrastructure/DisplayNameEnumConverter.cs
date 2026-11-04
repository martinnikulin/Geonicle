using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Geonicle.Infrastructure
{
    /// <summary>
    /// Хранит перечисление в БД строкой: подпись [Display(Name)] члена, а если подписи нет, — его имя.
    /// Для столбцов, где в базе текст (например, КБ), а в коде нужен тип с ограниченным набором значений.
    /// NULL в БД остаётся null (EF не вызывает конвертер для NULL). Любое другое значение вне списка, в том числе пустая
    /// строка, — ошибка с понятным текстом: иначе значение молча исказилось бы или потерялось при сохранении строки.
    /// </summary>
    public class DisplayNameEnumConverter<TEnum> : ValueConverter<TEnum, string>
        where TEnum : struct, Enum
    {
        private static readonly IReadOnlyDictionary<TEnum, string> ToText = typeof(TEnum)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .ToDictionary(
                f => (TEnum)f.GetValue(null)!,
                f => f.GetCustomAttribute<DisplayAttribute>()?.Name ?? f.Name);

        private static readonly IReadOnlyDictionary<string, TEnum> FromText =
            ToText.ToDictionary(p => p.Value, p => p.Key);

        public DisplayNameEnumConverter()
            : base(value => Write(value), text => Read(text))
        {
        }

        public static string Write(TEnum value) => ToText[value];

        public static TEnum Read(string text)
        {
            return FromText.TryGetValue(text.Trim(), out var value)
                ? value
                : throw new InvalidOperationException(
                    $"Неизвестное значение «{text}» для {typeof(TEnum).Name}. Допустимы: {string.Join(", ", FromText.Keys)}.");
        }
    }
}
