using Xunit;

namespace Geonicle.Data.Tests.Integration;

/// <summary>
/// Тест на реальном SQL Server. Выполняется только если задана переменная окружения GEONICLE_TEST_CONNECTION
/// (строка подключения к ТЕСТОВОЙ базе); без неё пропускается (так и на GitHub Actions).
/// </summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public const string VariableName = "GEONICLE_TEST_CONNECTION";

    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(VariableName)))
            Skip = $"Задайте переменную окружения {VariableName} (строка подключения к тестовой БД).";
    }
}
