using Data.Models;
using FluentValidation;

namespace Geonicle.Validation
{
    /// <summary>
    /// Правила проверки слоя литологии. В БД глубины «От» и «До» уникальны в пределах скважины
    /// (уникальные индексы AX_lithology_1 и AX_lithology_2).
    /// </summary>
    public class LithologyValidator : RowValidator<Lithology>
    {
        public LithologyValidator()
        {
            RuleFor(l => l.DepthFrom)
                .GreaterThanOrEqualTo(0).WithMessage("Глубина «От» не может быть отрицательной.");

            RuleFor(l => l.DepthTo)
                .GreaterThan(l => l.DepthFrom).WithMessage("Глубина «До» должна быть больше «От».");

            RuleFor(l => l.Th)
                .GreaterThanOrEqualTo(0).WithMessage("Мощность не может быть отрицательной.");

            RuleFor(l => l.ThNorm)
                .GreaterThanOrEqualTo(0).WithMessage("Мощность (норм) не может быть отрицательной.");

            RuleFor(l => l.Core)
                .GreaterThanOrEqualTo(0).When(l => l.Core != null).WithMessage("Керн не может быть отрицательным.");

            RuleFor(l => l.CoreNorm)
                .GreaterThanOrEqualTo(0).When(l => l.CoreNorm != null).WithMessage("Керн (норм) не может быть отрицательным.");

            RuleFor(l => l.CoreProc)
                .InclusiveBetween(0, 100).When(l => l.CoreProc != null).WithMessage("Керн, % должен быть от 0 до 100.");

            RuleFor(l => l)
                .Must(l => l.Borehole == null || l.Borehole.Lithologies.Count(o => o.DepthFrom == l.DepthFrom) <= 1)
                .WithMessage("Слой с такой глубиной «От» уже есть в этой скважине.");

            RuleFor(l => l)
                .Must(l => l.Borehole == null || l.Borehole.Lithologies.Count(o => o.DepthTo == l.DepthTo) <= 1)
                .WithMessage("Слой с такой глубиной «До» уже есть в этой скважине.");
        }

        public override string Describe(Lithology lithology) => $"Литология {lithology.DepthFrom}–{lithology.DepthTo}";
    }
}
