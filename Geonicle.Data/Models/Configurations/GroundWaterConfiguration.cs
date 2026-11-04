#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности GroundWater на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class GroundWaterConfiguration : IEntityTypeConfiguration<GroundWater>
{
    public void Configure(EntityTypeBuilder<GroundWater> entity)
    {
        entity.HasKey(e => e.BoreholeId);

        entity.ToTable("GroundWater");

        entity.Property(e => e.BoreholeId).ValueGeneratedNever();
        entity.Property(e => e.Depth).HasColumnType("numeric(18, 2)");

        entity.HasOne(d => d.Borehole).WithOne(p => p.GroundWater)
            .HasForeignKey<GroundWater>(d => d.BoreholeId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_GroundWater_Boreholes");
    }
}
