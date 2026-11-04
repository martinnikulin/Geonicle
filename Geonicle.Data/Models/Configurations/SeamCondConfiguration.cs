#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности SeamCond на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class SeamCondConfiguration : IEntityTypeConfiguration<SeamCond>
{
    public void Configure(EntityTypeBuilder<SeamCond> entity)
    {
        entity.HasKey(e => new { e.SeamId, e.ConditionId }).HasName("PK_Seamconds");

        entity.HasIndex(e => e.SeamId, "IX_Seamconds");

        entity.HasIndex(e => e.ConditionId, "IX_Seamconds_1");

        entity.HasIndex(e => e.RealCondId, "IX_Seamconds_2");

        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_SeamConds_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_SeamConds_DateModified")
            .HasColumnType("datetime");

        entity.HasOne(d => d.Seam).WithMany(p => p.SeamConds)
            .HasForeignKey(d => d.SeamId)
            .HasConstraintName("Seams_Seamconds");
    }
}
