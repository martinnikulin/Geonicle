using FluentValidation;
using FluentValidation.Results;

namespace Geonicle.Validation
{
    /// <summary>Валидатор строки без привязки к типу: нужен, чтобы проверять сущности из ChangeTracker.</summary>
    public interface IRowValidator
    {
        Type EntityType { get; }

        /// <summary>Как назвать строку в сообщении об ошибке, например «Скважина «A-1»».</summary>
        string Describe(object entity);

        ValidationResult Validate(object entity);

        /// <summary>Причины, по которым строку нельзя удалить (пусто — можно).</summary>
        IEnumerable<string> ValidateDelete(object entity);
    }

    /// <summary>
    /// Основа валидатора сущности. Наследники описывают правила в конструкторе (RuleFor...)
    /// и по желанию переопределяют Describe.
    /// </summary>
    public abstract class RowValidator<T> : AbstractValidator<T>, IRowValidator
        where T : class
    {
        public virtual string Describe(T entity) => typeof(T).Name;

        /// <summary>Причины, по которым строку нельзя удалить; по умолчанию удалять можно.</summary>
        public virtual IEnumerable<string> ValidateDelete(T entity) => Array.Empty<string>();

        Type IRowValidator.EntityType => typeof(T);

        string IRowValidator.Describe(object entity) => Describe((T)entity);

        ValidationResult IRowValidator.Validate(object entity) => Validate((T)entity);

        IEnumerable<string> IRowValidator.ValidateDelete(object entity) => ValidateDelete((T)entity);
    }
}
