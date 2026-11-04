#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Sample на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class SampleConfiguration : IEntityTypeConfiguration<Sample>
{
    public void Configure(EntityTypeBuilder<Sample> entity)
    {
        entity
            .HasNoKey()
            .ToView("Samples");

        entity.Property(e => e.DepthFrom).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.DepthTo).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.Nlayer)
            .HasMaxLength(20)
            .HasColumnName("NLayer");
        entity.Property(e => e.Nsample)
            .HasMaxLength(100)
            .HasColumnName("NSample");
        entity.Property(e => e.SampleType).HasMaxLength(50);
    }
}
