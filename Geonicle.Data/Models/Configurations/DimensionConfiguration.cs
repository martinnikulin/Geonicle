#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Dimension на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class DimensionConfiguration : IEntityTypeConfiguration<Dimension>
{
    public void Configure(EntityTypeBuilder<Dimension> entity)
    {
        entity.Property(e => e.Dim)
            .IsRequired()
            .HasMaxLength(50);
        entity.Property(e => e.Scale).HasDefaultValue(0, "DF_Dimensions_scale");
        entity.Property(e => e.Section)
            .IsRequired()
            .HasMaxLength(50);
    }
}
