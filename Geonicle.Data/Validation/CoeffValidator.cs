using Data.Models;
using FluentValidation;

namespace Geonicle.Validation
{
    /// <summary>Правила проверки коэффициента. Прослой, тип и марка могут быть 0 (не заданы).</summary>
    public class CoeffValidator : RowValidator<Coeff>
    {
        public CoeffValidator()
        {
            // В БД сочетание (пласт, прослой, марка, тип) — составной ключ, повторяться не может.
            RuleFor(c => c)
                .Must(c => c.Seam == null || c.Seam.Coeffs.Count(other =>
                    other.InterbedId == c.InterbedId && other.CoalType == c.CoalType && other.CoalGrade == c.CoalGrade) <= 1)
                .WithMessage("Коэффициент с таким прослоем, типом и маркой уже есть у этого пласта.");
        }

        public override string Describe(Coeff coeff) =>
            string.IsNullOrWhiteSpace(coeff.Seam?.Description) ? "Коэффициент" : $"Коэффициент пласта «{coeff.Seam.Description}»";
    }
}
