using Data.Models;
using Microsoft.EntityFrameworkCore;

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

        public static bool HasRules => Rules.Count > 0;

        public static void Run(
            IReadOnlyList<(object Entity, EntityState State)> changes,
            ChangePhase phase,
            GDB52Context db)
        {
            foreach (var rule in Rules)
            {
                if (rule.Phase != phase)
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
