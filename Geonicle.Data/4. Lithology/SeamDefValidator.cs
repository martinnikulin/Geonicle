using Data.Models;
using FluentValidation;
using Geonicle.Data.Services;

namespace Geonicle.Validation
{
    /// <summary>
    /// Правила проверки пластопересечения. Строки пластопересечений в гриде не создаются,
    /// проверяются только правки: стопки, КБ и комментарий (длины как в схеме БД).
    /// </summary>
    public class SeamDefValidator : RowValidator<SeamDef>
    {
        public SeamDefValidator()
        {
            RuleFor(s => s.DrawTo)
                .GreaterThanOrEqualTo(s => s.DrawFrom).WithMessage("«Стопки до» не может быть меньше «Стопки от».");

            RuleFor(s => s.Kb)
                .Must(kb => string.IsNullOrEmpty(kb) || HoleSeamKb.Values.Contains(kb))
                .WithMessage("КБ должен быть одним из значений списка.");

            RuleFor(s => s.Comments)
                .MaximumLength(20).WithMessage("Комментарий не может быть длиннее 20 символов.");
        }

        public override string Describe(SeamDef seam) =>
            string.IsNullOrWhiteSpace(seam.Seam?.Description) ? "Пластопересечение" : $"Пластопересечение «{seam.Seam.Description}»";
    }
}
