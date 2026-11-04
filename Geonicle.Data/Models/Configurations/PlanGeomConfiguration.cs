#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности PlanGeom на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class PlanGeomConfiguration : IEntityTypeConfiguration<PlanGeom>
{
    public void Configure(EntityTypeBuilder<PlanGeom> entity)
    {
        entity.HasKey(e => e.Id).HasName("PK_PlanGeoms_1");

        entity.ToTable(tb => tb.HasTrigger("AfterUpdatePlanGeoms"));

        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_PlanGeoms_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_PlanGeoms_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.FeatureName).HasMaxLength(100);
        entity.Property(e => e.InterbedId).HasDefaultValue(0, "DF__PlanGeoms__Inter__70A02EBB");
        entity.Property(e => e.ObjectId)
            .IsRequired()
            .HasMaxLength(10)
            .IsFixedLength();
        entity.Property(e => e.PartId).HasDefaultValue(1, "DF_PlanGeoms_PartId");

        entity.HasOne(d => d.Seam).WithMany(p => p.PlanGeoms)
            .HasForeignKey(d => d.SeamId)
            .HasConstraintName("FK_PlanGeoms_Seams");
    }
}
