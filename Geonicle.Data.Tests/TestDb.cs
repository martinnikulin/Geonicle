using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Geonicle.Data.Tests;

/// <summary>Своя база InMemory на каждый тест и фабрика контекстов для сервисов.</summary>
public sealed class TestDb : IDbContextFactory<GDB52Context>
{
    private readonly DbContextOptions<GDB52Context> options =
        new DbContextOptionsBuilder<GDB52Context>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    public GDB52Context CreateDbContext() => new(options);
}
