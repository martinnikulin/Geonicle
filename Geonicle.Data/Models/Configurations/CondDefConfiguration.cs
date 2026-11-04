#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности CondDef на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class CondDefConfiguration : IEntityTypeConfiguration<CondDef>
{
    public void Configure(EntityTypeBuilder<CondDef> entity)
    {
        entity.Property(e => e.Actual).HasDefaultValue(true, "DF_Conddefs_1_actual");
        entity.Property(e => e.CondName).HasMaxLength(50);
        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_GDCS_CondDefs_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_GDCS_CondDefs_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.Section)
            .IsRequired()
            .HasMaxLength(50);
    }
}
