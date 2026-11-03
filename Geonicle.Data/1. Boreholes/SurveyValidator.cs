using Data.Models;
using FluentValidation;

namespace Geonicle.Validation
{
    /// <summary>Правила проверки замера инклинометрии.</summary>
    public class SurveyValidator : RowValidator<Survey>
    {
        public SurveyValidator()
        {
            RuleFor(s => s.Depth)
                .GreaterThanOrEqualTo(0).WithMessage("Глубина замера не может быть отрицательной.");

            RuleFor(s => s.Dip)
                .InclusiveBetween(0, 90).WithMessage("Угол должен быть от 0 до 90.");

            RuleFor(s => s.Azimuth)
                .InclusiveBetween(0, 360).WithMessage("Азимут должен быть от 0 до 360.");

            // В БД пара (скважина, глубина) уникальна (индекс IX_Survey).
            RuleFor(s => s)
                .Must(s => s.Borehole == null || s.Borehole.Surveys.Count(other => other.Depth == s.Depth) <= 1)
                .WithMessage("Замер с такой глубиной уже есть у этой скважины.");
        }

        public override string Describe(Survey survey)
        {
            var borehole = survey.Borehole?.HoleName;
            return string.IsNullOrWhiteSpace(borehole)
                ? $"Замер на глубине {survey.Depth:0.##}"
                : $"Замер скважины «{borehole}», глубина {survey.Depth:0.##}";
        }
    }
}
