#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности QueryParam на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class QueryParamConfiguration : IEntityTypeConfiguration<QueryParam>
{
    public void Configure(EntityTypeBuilder<QueryParam> entity)
    {
        entity
            .HasNoKey()
            .ToView("QueryParams");

        entity.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(50);
        entity.Property(e => e.KeyField)
            .IsRequired()
            .HasMaxLength(50);
        entity.Property(e => e.Listfield)
            .IsRequired()
            .HasMaxLength(50);
        entity.Property(e => e.Nparam).HasColumnName("NParam");
        entity.Property(e => e.ParamType)
            .IsRequired()
            .HasMaxLength(10);
        entity.Property(e => e.RefTable)
            .IsRequired()
            .HasMaxLength(50);
    }
}
