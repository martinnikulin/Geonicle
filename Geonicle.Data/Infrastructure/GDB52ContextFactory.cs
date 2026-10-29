using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Geonicle.Infrastructure
{
    /// <summary>
    /// Создаёт контексты через конструктор со строкой подключения.
    /// Стандартный AddDbContextFactory здесь не подходит: у GDB52Context несколько конструкторов,
    /// и активация через DI не может выбрать между ними.
    /// </summary>
    public class GDB52ContextFactory : IDbContextFactory<GDB52Context>
    {
        private readonly string connectionString;

        public GDB52ContextFactory(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public GDB52Context CreateDbContext() => new GDB52Context(connectionString);
    }
}
