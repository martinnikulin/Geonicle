#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности HoleGroup на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class HoleGroupConfiguration : IEntityTypeConfiguration<HoleGroup>
{
    public void Configure(EntityTypeBuilder<HoleGroup> entity)
    {
        entity.HasKey(e => e.GroupId).HasName("PK_Holegroups");

        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Holegroups_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Holegroups_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.Description).HasMaxLength(50);
    }
}
