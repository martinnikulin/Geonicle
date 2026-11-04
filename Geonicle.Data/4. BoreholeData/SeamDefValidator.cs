using Data.Models;
using FluentValidation;
using Geonicle.Data.Services;

namespace Geonicle.Validation
{
    /// <summary>
    /// Правила проверки пластопересечения: стопки, КБ и комментарий (длины как в схеме БД), границы и уникальность.
    /// В БД границы уникальны в пределах скважины и прослоя (индексы AX_Holeseams_1 и AX_Holeseams_2);
    /// кроме того, пластопересечения одного прослоя не должны пересекаться.
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

            RuleFor(s => s)
                .Must(s => s.DepthFrom == null || s.DepthTo == null || s.DepthFrom < s.DepthTo)
                .WithMessage("Нижняя граница пластопересечения должна быть глубже верхней.");

            RuleFor(s => s)
                .Must(s => Neighbours(s).All(o => o.DepthFromId != s.DepthFromId))
                .WithMessage("В этом прослое уже есть пластопересечение с такой верхней границей.");

            RuleFor(s => s)
                .Must(s => Neighbours(s).All(o => o.DepthToId != s.DepthToId))
                .WithMessage("В этом прослое уже есть пластопересечение с такой нижней границей.");

            RuleFor(s => s)
                .Must(s => s.DepthFrom == null || s.DepthTo == null
                    || !Neighbours(s).Any(o => o.DepthFrom < s.DepthTo && s.DepthFrom < o.DepthTo))
                .WithMessage("Пластопересечение пересекается с другим пластопересечением этого прослоя.");
        }

        /// <summary>Другие пластопересечения той же скважины и прослоя (из коллекции скважины, загруженной экраном).</summary>
        private static IEnumerable<SeamDef> Neighbours(SeamDef seam) =>
            seam.Borehole?.SeamDefs.Where(o => !ReferenceEquals(o, seam) && o.InterbedId == seam.InterbedId)
            ?? Enumerable.Empty<SeamDef>();

        public override string Describe(SeamDef seam) =>
            string.IsNullOrWhiteSpace(seam.Seam?.Description) ? "Пластопересечение" : $"Пластопересечение «{seam.Seam.Description}»";
    }
}
