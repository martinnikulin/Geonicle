using Data.Models;
using FluentValidation;

namespace Geonicle.Validation
{
    /// <summary>Правила проверки скважины в разведочной линии.</summary>
    public class LineHoleValidator : RowValidator<LineHole>
    {
        public LineHoleValidator()
        {
            RuleFor(h => h.BoreholeId)
                .GreaterThan(0).WithMessage("Выберите скважину.");

            // Ключ линии у строки в новой линии становится известен только при сохранении, поэтому
            // принадлежность линии определяем по ключу или по связи Line.
            RuleFor(h => h)
                .Must(h => h.LineId != 0 || h.Line != null)
                .WithMessage("Строка должна принадлежать линии.");

            RuleFor(h => h.SpreadAzimuth)
                .InclusiveBetween(0, 360).WithMessage("Азимут проекции должен быть от 0 до 360.");

            // Одна скважина не должна встречаться в линии дважды; «не выбрана» (0) здесь не считаем.
            RuleFor(h => h)
                .Must(h => h.BoreholeId == 0 || h.Line == null || h.Line.LineHoles.Count(other => other.BoreholeId == h.BoreholeId) <= 1)
                .WithMessage("Эта скважина уже есть в линии.");
        }

        public override string Describe(LineHole lineHole) =>
            string.IsNullOrWhiteSpace(lineHole.Line?.Description)
                ? "Скважина в линии"
                : $"Скважина в линии «{lineHole.Line.Description}»";
    }
}
