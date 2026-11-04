#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности ChangedRecord на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class ChangedRecordConfiguration : IEntityTypeConfiguration<ChangedRecord>
{
    public void Configure(EntityTypeBuilder<ChangedRecord> entity)
    {
        entity.HasKey(e => e.Id).HasName("PK_Tables");

        entity.ToTable(tb => tb.HasTrigger("AfterUpdateChangedRecords"));

        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_GDCS_ChangedRecords_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_GDCS_ChangedRecords_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.TableName)
            .IsRequired()
            .HasMaxLength(50);
    }
}
