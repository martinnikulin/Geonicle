#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Rock на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class RockConfiguration : IEntityTypeConfiguration<Rock>
{
    public void Configure(EntityTypeBuilder<Rock> entity)
    {
        entity
            .HasNoKey()
            .ToView("Rocks");

        entity.Property(e => e.Ad).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.AutocadPattern).HasMaxLength(50);
        entity.Property(e => e.Dda).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.Description).HasMaxLength(100);
    }
}
