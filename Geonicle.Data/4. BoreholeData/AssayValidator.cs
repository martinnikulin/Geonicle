using Data.Models;
using FluentValidation;

namespace Geonicle.Validation
{
    /// <summary>Правила проверки анализа пробы (Assays); длина как в схеме БД.</summary>
    public class AssayValidator : RowValidator<Assay>
    {
        public AssayValidator()
        {
            RuleFor(a => a.Val)
                .MaximumLength(200).WithMessage("Значение показателя не может быть длиннее 200 символов.");
        }

        public override string Describe(Assay assay) => $"Анализ показателя {assay.VarId}";
    }
}
