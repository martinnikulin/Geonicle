#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности LineHolesDistinct на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class LineHolesDistinctConfiguration : IEntityTypeConfiguration<LineHolesDistinct>
{
    public void Configure(EntityTypeBuilder<LineHolesDistinct> entity)
    {
        entity
            .HasNoKey()
            .ToView("LineHolesDistinct");
    }
}
