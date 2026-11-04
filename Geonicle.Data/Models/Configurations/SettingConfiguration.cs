#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Setting на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> entity)
    {
        entity.Property(e => e.N).HasDefaultValue(1, "DF_Settings_N");
        entity.Property(e => e.ParamName).HasMaxLength(50);
        entity.Property(e => e.Section).HasMaxLength(50);
    }
}
