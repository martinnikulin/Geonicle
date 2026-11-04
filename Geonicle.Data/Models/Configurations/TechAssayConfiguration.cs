#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности TechAssay на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class TechAssayConfiguration : IEntityTypeConfiguration<TechAssay>
{
    public void Configure(EntityTypeBuilder<TechAssay> entity)
    {
        entity
            .HasNoKey()
            .ToView("TechAssays");

        entity.Property(e => e.XCoal).HasColumnName("xCoal");
        entity.Property(e => e.YCoal).HasColumnName("yCoal");
    }
}
