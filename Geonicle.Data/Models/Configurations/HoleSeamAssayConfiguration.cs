#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности HoleSeamAssay на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class HoleSeamAssayConfiguration : IEntityTypeConfiguration<HoleSeamAssay>
{
    public void Configure(EntityTypeBuilder<HoleSeamAssay> entity)
    {
        entity
            .HasNoKey()
            .ToView("HoleSeamAssays");

        entity.Property(e => e.AutocadText)
            .IsRequired()
            .HasMaxLength(100);
        entity.Property(e => e.Ngroup).HasColumnName("NGroup");
        entity.Property(e => e.Nvar).HasColumnName("NVar");
        entity.Property(e => e.Val).HasMaxLength(50);
    }
}
