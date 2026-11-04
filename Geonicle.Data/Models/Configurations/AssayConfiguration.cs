#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Assay на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class AssayConfiguration : IEntityTypeConfiguration<Assay>
{
    public void Configure(EntityTypeBuilder<Assay> entity)
    {
        entity.HasKey(e => new { e.SampleId, e.VarId });

        entity.ToTable(tb =>
            {
                tb.HasTrigger("AfterInsertAssays");
                tb.HasTrigger("AfterUpdateAssays");
            });

        entity.HasIndex(e => e.SampleId, "IX_Assays");

        entity.HasIndex(e => e.VarId, "IX_Assays_1");

        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Assays_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Assays_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.Val).HasMaxLength(200);

        entity.HasOne(d => d.Sample).WithMany(p => p.Assays)
            .HasForeignKey(d => d.SampleId)
            .HasConstraintName("FK_Assays_Samples");
    }
}
