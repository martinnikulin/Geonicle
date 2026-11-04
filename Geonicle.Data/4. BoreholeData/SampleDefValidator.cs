using Data.Models;
using FluentValidation;

namespace Geonicle.Validation
{
    /// <summary>Правила проверки пробы (SampleDef); длины как в схеме БД.</summary>
    public class SampleDefValidator : RowValidator<SampleDef>
    {
        public SampleDefValidator()
        {
            RuleFor(s => s.DepthFrom)
                .GreaterThanOrEqualTo(0).WithMessage("Глубина «От» не может быть отрицательной.");

            RuleFor(s => s.DepthTo)
                .GreaterThan(s => s.DepthFrom).WithMessage("Глубина «До» должна быть больше «От».");

            RuleFor(s => s.Nsample)
                .MaximumLength(200).WithMessage("Номер пробы не может быть длиннее 200 символов.");
        }

        public override string Describe(SampleDef sample) =>
            string.IsNullOrWhiteSpace(sample.Nsample) ? "Проба" : $"Проба «{sample.Nsample}»";
    }
}
