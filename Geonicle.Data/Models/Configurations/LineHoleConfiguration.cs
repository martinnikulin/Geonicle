#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности LineHole на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class LineHoleConfiguration : IEntityTypeConfiguration<LineHole>
{
    public void Configure(EntityTypeBuilder<LineHole> entity)
    {
        entity.HasIndex(e => e.LineId, "IX_LineHoles_1");

        entity.HasIndex(e => e.BoreholeId, "IX_LineHoles_2");

        entity.Property(e => e.N).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.Node).HasDefaultValue(true, "DF_LineHoles_Node");
        entity.Property(e => e.OnProfile).HasDefaultValue(true, "DF_LineHoles_OnProfile");
        entity.Property(e => e.SpreadAzimuth).HasColumnType("decimal(18, 2)");

        entity.HasOne(d => d.Borehole).WithMany(p => p.LineHoles)
            .HasForeignKey(d => d.BoreholeId)
            .HasConstraintName("FK_LineHoles_Holes");

        entity.HasOne(d => d.Line).WithMany(p => p.LineHoles)
            .HasForeignKey(d => d.LineId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_LineHoles_Surveylines");
    }
}
