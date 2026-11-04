#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности AssayVar на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class AssayVarConfiguration : IEntityTypeConfiguration<AssayVar>
{
    public void Configure(EntityTypeBuilder<AssayVar> entity)
    {
        entity
            .HasNoKey()
            .ToView("AssayVars");

        entity.Property(e => e.AutocadText)
            .IsRequired()
            .HasMaxLength(100);
        entity.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(500);
        entity.Property(e => e.Dimension)
            .IsRequired()
            .HasMaxLength(100);
    }
}
