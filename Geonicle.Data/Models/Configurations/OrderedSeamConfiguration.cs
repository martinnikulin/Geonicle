#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности OrderedSeam на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class OrderedSeamConfiguration : IEntityTypeConfiguration<OrderedSeam>
{
    public void Configure(EntityTypeBuilder<OrderedSeam> entity)
    {
        entity
            .HasNoKey()
            .ToView("OrderedSeams");

        entity.Property(e => e.Nseam).HasColumnName("NSeam");
        entity.Property(e => e.SeamId).ValueGeneratedOnAdd();
        entity.Property(e => e.SeamName).HasMaxLength(50);
        entity.Property(e => e.ShortName).HasMaxLength(50);
    }
}
