using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Geonicle.Rules
{
    /// <summary>Когда правило выполняется относительно записи в БД.</summary>
    public enum ChangePhase
    {
        /// <summary>До SaveChanges: правило добавляет или меняет сущности, они уходят в тот же запрос.</summary>
        BeforeSave,

        /// <summary>После первой записи, в той же транзакции: уже известны ключи, созданные БД (identity).</summary>
        AfterSave
    }

    /// <summary>
    /// Правило, которое выполняется при добавлении, изменении или удалении строки сущности.
    /// Все правила лежат в папке Rules, находятся автоматически (ChangeRuleCatalog), регистрировать их не нужно.
    /// </summary>
    public interface IChangeRule
    {
        Type EntityType { get; }

        EntityState State { get; }

        ChangePhase Phase { get; }

        void Apply(object entity, GDB52Context db);
    }

    /// <summary>Основа правила для сущности <typeparamref name="TEntity"/>.</summary>
    public abstract class ChangeRule<TEntity> : IChangeRule
        where TEntity : class
    {
        /// <summary>Added, Modified или Deleted.</summary>
        public abstract EntityState State { get; }

        public virtual ChangePhase Phase => ChangePhase.BeforeSave;

        Type IChangeRule.EntityType => typeof(TEntity);

        void IChangeRule.Apply(object entity, GDB52Context db) => Apply((TEntity)entity, db);

        protected abstract void Apply(TEntity entity, GDB52Context db);
    }
}
