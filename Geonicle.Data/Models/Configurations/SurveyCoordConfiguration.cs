#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности SurveyCoord на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class SurveyCoordConfiguration : IEntityTypeConfiguration<SurveyCoord>
{
    public void Configure(EntityTypeBuilder<SurveyCoord> entity)
    {
        entity.HasKey(e => new { e.BoreholeId, e.Depth }).HasName("PK_SurveyCalcs");

        entity.Property(e => e.Depth).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.Azimuth).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.Dip).HasColumnType("decimal(18, 2)");

        entity.HasOne(d => d.Borehole).WithMany(p => p.SurveyCoords)
            .HasForeignKey(d => d.BoreholeId)
            .HasConstraintName("FK_SurveyCoords_Boreholes");
    }
}
