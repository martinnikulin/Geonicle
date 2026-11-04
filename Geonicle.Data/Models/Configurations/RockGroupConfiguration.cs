#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности RockGroup на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class RockGroupConfiguration : IEntityTypeConfiguration<RockGroup>
{
    public void Configure(EntityTypeBuilder<RockGroup> entity)
    {
        entity
            .HasNoKey()
            .ToView("RockGroups");

        entity.Property(e => e.DateCreated).HasColumnType("datetime");
        entity.Property(e => e.DateModified).HasColumnType("datetime");
        entity.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(20);
    }
}
