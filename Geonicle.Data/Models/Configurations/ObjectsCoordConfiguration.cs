#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности ObjectsCoord на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class ObjectsCoordConfiguration : IEntityTypeConfiguration<ObjectsCoord>
{
    public void Configure(EntityTypeBuilder<ObjectsCoord> entity)
    {
        entity.Property(e => e.N).HasMaxLength(10);
        entity.Property(e => e.ObjectName).HasMaxLength(50);
        entity.Property(e => e.X).HasColumnType("numeric(18, 2)");
        entity.Property(e => e.Y).HasColumnType("numeric(18, 2)");
    }
}
