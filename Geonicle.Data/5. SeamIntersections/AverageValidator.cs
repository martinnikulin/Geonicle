using Data.Models;
using FluentValidation;

namespace Geonicle.Validation
{
    /// <summary>
    /// Правила проверки среднего по пласту (Averages). Ключ: прослой, пласт, тип, марка и показатель;
    /// у новой строки тип и марка могут быть 0 (не заданы), а показатель выбирается обязательно.
    /// Повтор ключа правилами не проверяется: EF не отслеживает две строки с одним ключом, и EntityValidation
    /// сообщает об этом отдельным понятным сообщением.
    /// </summary>
    public class AverageValidator : RowValidator<Average>
    {
        public AverageValidator()
        {
            RuleFor(a => a.VarId)
                .GreaterThan(0).WithMessage("Выберите показатель.");
        }

        public override string Describe(Average average) =>
            string.IsNullOrWhiteSpace(average.Seam?.Description) ? "Среднее" : $"Среднее по пласту «{average.Seam.Description}»";
    }
}
