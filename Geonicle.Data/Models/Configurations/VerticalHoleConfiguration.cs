#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности VerticalHole на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class VerticalHoleConfiguration : IEntityTypeConfiguration<VerticalHole>
{
    public void Configure(EntityTypeBuilder<VerticalHole> entity)
    {
        entity
            .HasNoKey()
            .ToView("VerticalHoles");
    }
}
