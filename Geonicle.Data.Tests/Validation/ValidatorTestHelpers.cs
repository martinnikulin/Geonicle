using FluentValidation.Results;

namespace Geonicle.Data.Tests.Validation;

internal static class ValidatorTestHelpers
{
    public static bool HasMessage(this ValidationResult result, string message) =>
        result.Errors.Any(e => e.ErrorMessage == message);

    public static string Messages(this ValidationResult result) =>
        string.Join(" | ", result.Errors.Select(e => e.ErrorMessage));
}
