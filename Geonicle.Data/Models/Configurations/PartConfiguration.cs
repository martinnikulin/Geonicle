#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Part на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class PartConfiguration : IEntityTypeConfiguration<Part>
{
    public void Configure(EntityTypeBuilder<Part> entity)
    {
        entity.ToTable(tb => tb.HasTrigger("AfterInsertParts"));

        entity.HasIndex(e => e.SeamId, "IX_Parts");

        entity.Property(e => e.Chapter).HasMaxLength(50);
        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Parts_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Parts_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(150)
            .HasDefaultValue("", "DF_Parts_description");
        entity.Property(e => e.N).HasDefaultValue(1, "DF_Parts_part_nn");
        entity.Property(e => e.Npart)
            .IsRequired()
            .HasMaxLength(12)
            .HasDefaultValue("", "DF_Parts_npart")
            .HasColumnName("NPart");

        entity.HasOne(d => d.Seam).WithMany(p => p.Parts)
            .HasForeignKey(d => d.SeamId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("Seams_Parts");
    }
}
