#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности InclinedHole на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class InclinedHoleConfiguration : IEntityTypeConfiguration<InclinedHole>
{
    public void Configure(EntityTypeBuilder<InclinedHole> entity)
    {
        entity
            .HasNoKey()
            .ToView("InclinedHoles");
    }
}
