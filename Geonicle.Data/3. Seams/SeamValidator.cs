using Data.Models;
using FluentValidation;

namespace Geonicle.Validation
{
    /// <summary>Правила проверки пласта.</summary>
    public class SeamValidator : RowValidator<Seam>
    {
        public SeamValidator()
        {
            RuleFor(s => s.Description)
                .NotEmpty().WithMessage("Название пласта обязательно.")
                .MaximumLength(50).WithMessage("Название пласта не может быть длиннее 50 символов.");

            RuleFor(s => s.ShortName)
                .NotEmpty().WithMessage("Короткое название пласта обязательно.")
                .MaximumLength(50).WithMessage("Короткое название пласта не может быть длиннее 50 символов.");

            RuleFor(s => s.GroupSeamId)
                .MaximumLength(12).WithMessage("Код группы пласта не может быть длиннее 12 символов.");

            RuleFor(s => s.AdRoof).InclusiveBetween(0, 100).WithMessage("Ad кровли должен быть от 0 до 100.");
            RuleFor(s => s.AdBed).InclusiveBetween(0, 100).WithMessage("Ad почвы должен быть от 0 до 100.");
            RuleFor(s => s.DdaRoof).InclusiveBetween(0, 100).WithMessage("Dda кровли должен быть от 0 до 100.");
            RuleFor(s => s.DdaBed).InclusiveBetween(0, 100).WithMessage("Dda почвы должен быть от 0 до 100.");

            RuleFor(s => s.N)
                .GreaterThanOrEqualTo(0d).When(s => s.N.HasValue)
                .WithMessage("N пласта не может быть отрицательным.");

            RuleFor(s => s.Strata)
                .GreaterThanOrEqualTo(0d).When(s => s.Strata.HasValue)
                .WithMessage("Страта не может быть отрицательной.");
        }

        public override string Describe(Seam seam) =>
            string.IsNullOrWhiteSpace(seam.Description) ? "Пласт (без названия)" : $"Пласт «{seam.Description}»";
    }
}
