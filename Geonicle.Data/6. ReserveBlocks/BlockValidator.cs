using Data.Models;
using FluentValidation;

namespace Geonicle.Validation
{
    /// <summary>
    /// Правила проверки блока. Имя в БД nchar(12): при чтении оно дополнено пробелами, поэтому длина считается без пробелов по краям.
    /// </summary>
    public class BlockValidator : RowValidator<Block>
    {
        public BlockValidator()
        {
            RuleFor(b => b.BlockName)
                .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Название блока обязательно.")
                .Must(name => name == null || name.Trim().Length <= 12).WithMessage("Название блока не может быть длиннее 12 символов.");
        }

        public override string Describe(Block block) =>
            string.IsNullOrWhiteSpace(block.BlockName) ? "Блок (без названия)" : $"Блок «{block.BlockName.Trim()}»";
    }

    /// <summary>Правила проверки части блока.</summary>
    public class BlockDetailValidator : RowValidator<BlockDetail>
    {
        public BlockDetailValidator()
        {
            RuleFor(d => d.Area).GreaterThanOrEqualTo(0).WithMessage("Площадь не может быть отрицательной.");
        }

        public override string Describe(BlockDetail detail) => "Часть блока";
    }

    /// <summary>
    /// Правила проверки скважины в блоке. Название записывается из списка пластопересечений («скважина, комментарий»)
    /// или вводится свободным текстом (строка без скважины); в БД до 50 символов.
    /// </summary>
    public class BlockUnitValidator : RowValidator<BlockUnit>
    {
        public BlockUnitValidator()
        {
            RuleFor(u => u.HoleName)
                .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Название скважины в блоке обязательно.")
                .Must(name => name == null || name.Length <= 50).WithMessage("Название скважины в блоке не может быть длиннее 50 символов.");
        }

        public override string Describe(BlockUnit unit) =>
            string.IsNullOrWhiteSpace(unit.HoleName) ? "Скважина в блоке" : $"Скважина в блоке «{unit.HoleName}»";
    }
}
