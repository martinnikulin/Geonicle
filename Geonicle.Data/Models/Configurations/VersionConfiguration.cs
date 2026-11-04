#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Version на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class VersionConfiguration : IEntityTypeConfiguration<Version>
{
    public void Configure(EntityTypeBuilder<Version> entity)
    {
        entity.ToTable(tb => tb.HasTrigger("AfterUpdateVersions"));

        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_Reserves_Versions_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_Reserves_Versions_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.Property(e => e.VersionName).HasMaxLength(50);
    }
}
