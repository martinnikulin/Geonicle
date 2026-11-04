#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Survey на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class SurveyConfiguration : IEntityTypeConfiguration<Survey>
{
    public void Configure(EntityTypeBuilder<Survey> entity)
    {
        entity.ToTable("Survey", tb =>
            {
                tb.HasTrigger("AfterDeleteInclination");
                tb.HasTrigger("AfterInsertInclination");
                tb.HasTrigger("AfterUpdateInclination");
            });

        entity.HasIndex(e => e.BoreholeId, "IX_Inclination_1");

        entity.HasIndex(e => new { e.BoreholeId, e.Depth }, "IX_Survey").IsUnique();

        entity.Property(e => e.Azimuth).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_Inclination_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_Inclination_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.Depth).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.Dip).HasColumnType("decimal(18, 2)");

        entity.HasOne(d => d.Borehole).WithMany(p => p.Surveys)
            .HasForeignKey(d => d.BoreholeId)
            .HasConstraintName("FK_Survey_Boreholes");
    }
}
