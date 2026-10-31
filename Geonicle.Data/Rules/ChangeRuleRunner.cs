using Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Reflection;

namespace Geonicle.Rules
{
    /// <summary>Находит правила и выполняет их для изменённых сущностей.</summary>
    internal static class ChangeRuleRunner
    {
        private static readonly IReadOnlyList<IChangeRule> Rules = typeof(IChangeRule).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(IChangeRule).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) != null)
            .Select(t => (IChangeRule)Activator.CreateInstance(t)!)
            .ToList();

        // Триггеры, существующие в БД, по строке подключения: узнаём один раз за время работы приложения.
        private static readonly ConcurrentDictionary<string, HashSet<string>> TriggersByDatabase = new();

        public static bool HasRules => Rules.Count > 0;

        public static async Task<HashSet<string>> GetTriggersAsync(GDB52Context db, CancellationToken cancellationToken)
        {
            if (!db.Database.IsRelational())
                return new HashSet<string>();

            var key = db.Database.GetConnectionString() ?? string.Empty;
            if (TriggersByDatabase.TryGetValue(key, out var cached))
                return cached;

            var names = await db.Database
                .SqlQueryRaw<string>("select name as Value from sys.triggers")
                .ToListAsync(cancellationToken);
            var triggers = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
            TriggersByDatabase[key] = triggers;
            return triggers;
        }

        public static void Run(
            IReadOnlyList<(object Entity, EntityState State)> changes,
            ChangePhase phase,
            HashSet<string> triggers,
            GDB52Context db)
        {
            foreach (var rule in Rules)
            {
                if (rule.Phase != phase)
                    continue;
                if (rule.ReplacesTrigger != null && triggers.Contains(rule.ReplacesTrigger))
                    continue;

                foreach (var (entity, state) in changes)
                {
                    if (state == rule.State && rule.EntityType.IsInstanceOfType(entity))
                        rule.Apply(entity, db);
                }
            }
        }
    }
}
