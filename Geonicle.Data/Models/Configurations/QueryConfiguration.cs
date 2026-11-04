#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Query на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class QueryConfiguration : IEntityTypeConfiguration<Query>
{
    public void Configure(EntityTypeBuilder<Query> entity)
    {
        entity
            .HasNoKey()
            .ToView("Queries");

        entity.Property(e => e.Description).HasMaxLength(100);
        entity.Property(e => e.QueryGroup).HasMaxLength(50);
        entity.Property(e => e.QueryId).ValueGeneratedOnAdd();
        entity.Property(e => e.Sqltext).HasColumnName("SQLText");
    }
}
