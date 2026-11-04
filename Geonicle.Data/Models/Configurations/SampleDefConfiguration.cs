#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности SampleDef на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class SampleDefConfiguration : IEntityTypeConfiguration<SampleDef>
{
    public void Configure(EntityTypeBuilder<SampleDef> entity)
    {
        entity.HasKey(e => e.SampleId)
            .HasName("aaaaasample_PK")
            .IsClustered(false);

        entity.ToTable(tb =>
            {
                tb.HasTrigger("AfterInsertSamples");
                tb.HasTrigger("AfterUpdateSamples");
            });

        entity.HasIndex(e => new { e.BoreholeId, e.DepthFrom }, "IX_Sample1");

        entity.HasIndex(e => new { e.BoreholeId, e.DepthTo }, "IX_Sample2");

        entity.HasIndex(e => new { e.SampleId, e.DepthFrom, e.DepthTo }, "IX_Samples").IsUnique();

        entity.Property(e => e.AdIndex).HasDefaultValue(1, "DF_Samples_Ad_index");
        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Samples_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Samples_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.DepthFrom).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.DepthTo).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.Nlayer)
            .HasMaxLength(20)
            .HasColumnName("NLayer");
        entity.Property(e => e.Nsample)
            .HasMaxLength(200)
            .HasColumnName("NSample");
        entity.Property(e => e.SampleType).HasMaxLength(50);
        entity.Property(e => e.Show).HasDefaultValue(true, "DF_Samples_show");

        entity.HasOne(d => d.Borehole).WithMany(p => p.SampleDefs)
            .HasForeignKey(d => d.BoreholeId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("Holes_Samples");
    }
}
