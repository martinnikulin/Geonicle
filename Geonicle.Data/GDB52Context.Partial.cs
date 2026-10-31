using Geonicle.Rules;
using Microsoft.EntityFrameworkCore;

namespace Data.Models;

// Ручные настройки модели; сгенерированный GDB52Context.cs не правим.
public partial class GDB52Context
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // В БД первичный ключ Coeffs составной (SeamId, InterbedId, CoalGrade, CoalType), а EF не даёт менять
        // значения ключа у существующей строки, поэтому в гриде нельзя было бы сменить прослой, тип или марку.
        // В модели приложения ключом считаем счётчик CoeffId (он уникален); схему БД это не меняет.
        modelBuilder.Entity<Coeff>().HasKey(e => e.CoeffId);

        // В данных Parts.Description бывает NULL, а скаффолд считает поле обязательным (EF падает с «Data is Null»).
        modelBuilder.Entity<Part>().Property(e => e.Description).IsRequired(false);

        // Скаффолд ставит ClientSetNull на обязательные связи. Когда строку подчинённого грида удаляют
        // из коллекции родителя (SelectedLine.LineHoles), EF считает связь разорванной и падает
        // ("association has been severed"). Каскад здесь действует только в трекере EF: схему БД и
        // её внешние ключи это не меняет, а подчинённая строка просто удаляется.
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            if (foreignKey.IsRequired && foreignKey.DeleteBehavior == DeleteBehavior.ClientSetNull)
                foreignKey.DeleteBehavior = DeleteBehavior.Cascade;
        }
    }

    /// <summary>
    /// Сохранение с правилами изменений (папка Rules): правила до записи, запись, правила после записи
    /// (когда известны ключи, созданные БД) и вторая запись; всё в одной транзакции.
    /// </summary>
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        if (!ChangeRuleRunner.HasRules)
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        ChangeTracker.DetectChanges();
        var changes = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => (e.Entity, e.State))
            .ToList();
        if (changes.Count == 0)
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        ChangeRuleRunner.Run(changes, ChangePhase.BeforeSave, this);

        // Свою транзакцию открываем только в реляционной БД и только если внешней нет.
        var transaction = Database.IsRelational() && Database.CurrentTransaction == null
            ? await Database.BeginTransactionAsync(cancellationToken)
            : null;
        await using (transaction)
        {
            var count = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            ChangeRuleRunner.Run(changes, ChangePhase.AfterSave, this);
            if (ChangeTracker.HasChanges())
                count += await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            if (transaction != null)
                await transaction.CommitAsync(cancellationToken);
            return count;
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
        => throw new NotSupportedException("Правила изменений работают только в SaveChangesAsync.");
}
