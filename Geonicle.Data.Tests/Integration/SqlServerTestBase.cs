using Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;

namespace Geonicle.Data.Tests.Integration;

/// <summary>
/// Основа интеграционных тестов. Каждый тест работает внутри транзакции, которая никогда не фиксируется:
/// после теста всё откатывается и данные тестовой БД не меняются (но счётчики identity продвигаются).
/// Защита от ошибки: имя базы в строке подключения должно содержать «test», иначе тесты не запускаются.
/// </summary>
public abstract class SqlServerTestBase : IDisposable
{
    private readonly IDbContextTransaction transaction;

    protected SqlServerTestBase()
    {
        var connectionString = Environment.GetEnvironmentVariable(SqlServerFactAttribute.VariableName)
            ?? throw new InvalidOperationException($"Не задана переменная {SqlServerFactAttribute.VariableName}.");

        var database = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (!database.Contains("test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Интеграционные тесты отказываются работать с базой «{database}»: в её имени нет «test». " +
                "Укажите в GEONICLE_TEST_CONNECTION тестовую базу (с «test» в имени).");

        Db = new GDB52Context(connectionString);
        transaction = Db.Database.BeginTransaction();
    }

    protected GDB52Context Db { get; }

    /// <summary>Уникальное имя, чтобы тесты не конфликтовали с данными в базе.</summary>
    protected static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..30];

    protected Borehole NewBorehole() => new() { HoleName = Unique("it-hole"), HoleType = HoleType.Скважина, Depth = 10 };

    protected static Seam NewSeam() => new() { Description = Unique("it-seam"), ShortName = "it" };

    public void Dispose()
    {
        transaction.Dispose();   // без Commit: откат
        Db.Dispose();
    }
}
