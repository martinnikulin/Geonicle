#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности AssayGroup на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class AssayGroupConfiguration : IEntityTypeConfiguration<AssayGroup>
{
    public void Configure(EntityTypeBuilder<AssayGroup> entity)
    {
        entity
            .HasNoKey()
            .ToView("AssayGroups");

        entity.Property(e => e.DateCreated).HasColumnType("datetime");
        entity.Property(e => e.DateModified).HasColumnType("datetime");
        entity.Property(e => e.GroupId).ValueGeneratedOnAdd();
        entity.Property(e => e.GroupName).HasMaxLength(50);
    }
}
