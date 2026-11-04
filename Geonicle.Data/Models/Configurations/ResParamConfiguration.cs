#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности ResParam на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class ResParamConfiguration : IEntityTypeConfiguration<ResParam>
{
    public void Configure(EntityTypeBuilder<ResParam> entity)
    {
        entity.Property(e => e.Id).ValueGeneratedNever();
        entity.Property(e => e.Pbvr).HasColumnName("PBVR");
        entity.Property(e => e.Ptransport).HasColumnName("PTransport");
    }
}
