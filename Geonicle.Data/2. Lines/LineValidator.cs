using Data.Models;
using FluentValidation;

namespace Geonicle.Validation
{
    /// <summary>Правила проверки разведочной линии.</summary>
    public class LineValidator : RowValidator<Line>
    {
        public LineValidator()
        {
            RuleFor(l => l.Description)
                .NotEmpty().WithMessage("Название линии обязательно.")
                .MaximumLength(50).WithMessage("Название линии не может быть длиннее 50 символов.");

            RuleFor(l => l.LineType)
                .IsInEnum().WithMessage("Выберите тип линии.");

            RuleFor(l => l.N)
                .GreaterThanOrEqualTo(0).WithMessage("N линии не может быть отрицательным.");
        }

        public override string Describe(Line line) =>
            string.IsNullOrWhiteSpace(line.Description) ? "Линия (без названия)" : $"Линия «{line.Description}»";
    }
}
