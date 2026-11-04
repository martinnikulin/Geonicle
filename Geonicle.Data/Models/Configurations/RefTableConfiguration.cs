#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности RefTable на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class RefTableConfiguration : IEntityTypeConfiguration<RefTable>
{
    public void Configure(EntityTypeBuilder<RefTable> entity)
    {
        entity.HasKey(e => new { e.ItemId, e.RefId, e.Section }).HasName("PK_Reftable_1");

        entity.ToTable("RefTable");

        entity.HasIndex(e => e.ItemId, "AX_item_id");

        entity.HasIndex(e => e.ItemId, "AX_ref_id");

        entity.HasIndex(e => e.Section, "AX_section");

        entity.Property(e => e.Section)
            .HasMaxLength(50)
            .HasDefaultValue("boundary2", "DF_Reftable_section");
        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Reftable_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Reftable_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.Operator)
            .IsRequired()
            .HasMaxLength(1)
            .IsUnicode(false)
            .IsFixedLength()
            .HasDefaultValue("+", "DF_Reftable_operator");
    }
}
