using Data.Models;
using FluentValidation;

namespace Geonicle.Validation
{
    /// <summary>Правила проверки детали пласта. Номер и описание могут быть пустыми.</summary>
    public class PartValidator : RowValidator<Part>
    {
        public PartValidator()
        {
            RuleFor(p => p.Chapter)
                .MaximumLength(50).WithMessage("Приложение не может быть длиннее 50 символов.");

            RuleFor(p => p.Description)
                .MaximumLength(150).WithMessage("Описание детали не может быть длиннее 150 символов.");

            RuleFor(p => p.Npart)
                .MaximumLength(12).WithMessage("Номер детали не может быть длиннее 12 символов.");

            RuleFor(p => p.N)
                .GreaterThanOrEqualTo(0).WithMessage("N детали не может быть отрицательным.");
        }

        public override string Describe(Part part)
        {
            var title = string.IsNullOrWhiteSpace(part.Npart) ? "Деталь (без номера)" : $"Деталь «{part.Npart}»";
            return string.IsNullOrWhiteSpace(part.Seam?.Description) ? title : $"{title} пласта «{part.Seam.Description}»";
        }
    }
}
