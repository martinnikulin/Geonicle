using Data.Models;
using FluentValidation;

namespace Geonicle.Validation
{
    /// <summary>
    /// Правила проверки скважины. Один класс на сущность, текст сообщений по-русски прямо в правилах.
    /// Ограничения совпадают со схемой БД (HoleName: not null, 50 символов).
    /// </summary>
    public class BoreholeValidator : AbstractValidator<Borehole>
    {
        public BoreholeValidator()
        {
            RuleFor(b => b.HoleName)
                .NotEmpty().WithMessage("Название скважины обязательно.")
                .MaximumLength(50).WithMessage("Название скважины не может быть длиннее 50 символов.");

            RuleFor(b => b.Depth)
                .GreaterThanOrEqualTo(0).WithMessage("Глубина скважины не может быть отрицательной.");
        }
    }
}
