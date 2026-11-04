#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Composite на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class CompositeConfiguration : IEntityTypeConfiguration<Composite>
{
    public void Configure(EntityTypeBuilder<Composite> entity)
    {
        entity.HasKey(e => new { e.HoleSeamId, e.VarId }).HasName("PK_Composits");

        entity.Property(e => e.AvgCalc).HasDefaultValue(false, "DF__Composite__AvgCa__56E05CB8");
        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_Composites_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_Composites_DateModified")
            .HasColumnType("datetime");

        entity.HasOne(d => d.SeamDef).WithMany(p => p.Composites)
            .HasForeignKey(d => d.HoleSeamId)
            .HasConstraintName("FK_Composites_SeamDefs");
    }
}
