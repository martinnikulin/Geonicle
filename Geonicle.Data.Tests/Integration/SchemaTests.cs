using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Geonicle.Data.Tests.Integration;

public class SchemaTests : SqlServerTestBase
{
    [SqlServerFact]
    public void Every_mapped_entity_can_be_read_from_the_database()
    {
        // Ловит расхождения модели и БД: нет таблицы или столбца, NULL в обязательном поле и т. п.
        var setMethod = typeof(DbContext).GetMethods().Single(m => m.Name == nameof(DbContext.Set) && m.GetParameters().Length == 0);
        var takeMethod = typeof(Queryable).GetMethods()
            .Single(m => m.Name == nameof(Queryable.Take) && m.GetParameters()[1].ParameterType == typeof(int));

        var failures = new List<string>();
        foreach (var entityType in Db.Model.GetEntityTypes().Where(e => !e.IsOwned()))
        {
            try
            {
                var set = (IQueryable)setMethod.MakeGenericMethod(entityType.ClrType).Invoke(Db, null)!;
                var limited = (IQueryable)takeMethod.MakeGenericMethod(entityType.ClrType).Invoke(null, new object[] { set, 5 })!;
                foreach (var _ in limited)
                {
                }
            }
            catch (Exception ex)
            {
                failures.Add($"{entityType.ClrType.Name}: {(ex is TargetInvocationException { InnerException: { } inner } ? inner : ex).Message}");
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }
}
