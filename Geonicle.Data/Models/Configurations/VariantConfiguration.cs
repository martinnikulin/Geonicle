#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Variant на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class VariantConfiguration : IEntityTypeConfiguration<Variant>
{
    public void Configure(EntityTypeBuilder<Variant> entity)
    {
        entity
            .HasNoKey()
            .ToView("Variants");

        entity.Property(e => e.AshCondSum)
            .IsRequired()
            .HasMaxLength(50);
        entity.Property(e => e.Ashcond)
            .IsRequired()
            .HasMaxLength(50);
        entity.Property(e => e.Ashmax).HasMaxLength(50);
        entity.Property(e => e.Ashmin).HasMaxLength(50);
        entity.Property(e => e.Ibcond)
            .IsRequired()
            .HasMaxLength(50);
        entity.Property(e => e.Nvariant).HasColumnName("NVariant");
        entity.Property(e => e.ThCondSum)
            .IsRequired()
            .HasMaxLength(50);
        entity.Property(e => e.Thcond)
            .IsRequired()
            .HasMaxLength(50);
        entity.Property(e => e.Thmax).HasMaxLength(50);
        entity.Property(e => e.Thmin).HasMaxLength(50);
        entity.Property(e => e.VariantName1)
            .IsRequired()
            .HasMaxLength(152);
        entity.Property(e => e.VariantName2)
            .IsRequired()
            .HasMaxLength(150);
    }
}
