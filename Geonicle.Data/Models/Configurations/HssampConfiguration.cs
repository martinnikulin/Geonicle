#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Hssamp на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class HssampConfiguration : IEntityTypeConfiguration<Hssamp>
{
    public void Configure(EntityTypeBuilder<Hssamp> entity)
    {
        entity.HasKey(e => new { e.HoleSeamId, e.SampleId }).HasName("PK_Hssamps");

        entity.ToTable("HSSamps");

        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Hssamps_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Hssamps_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.SampStage).HasDefaultValue(1, "DF_Acceptedsamps_sampstage");

        entity.HasOne(d => d.SeamDef).WithMany(p => p.Hssamps)
            .HasForeignKey(d => d.HoleSeamId)
            .HasConstraintName("Holeseams_Hssamps");

        entity.HasOne(d => d.SampleDef).WithMany(p => p.Hssamps)
            .HasForeignKey(d => d.SampleId)
            .HasConstraintName("Samples_Hssamps");
    }
}
