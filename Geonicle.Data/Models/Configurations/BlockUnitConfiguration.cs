#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности BlockUnit на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class BlockUnitConfiguration : IEntityTypeConfiguration<BlockUnit>
{
    public void Configure(EntityTypeBuilder<BlockUnit> entity)
    {
        entity.Property(e => e.AdCoal).HasColumnType("numeric(18, 1)");
        entity.Property(e => e.AdFull).HasColumnType("numeric(18, 1)");
        entity.Property(e => e.AdR).HasDefaultValue(true);
        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Blockunits_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Blockunits_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.HoleName)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("");
        entity.Property(e => e.ThCoal).HasColumnType("numeric(18, 2)");
        entity.Property(e => e.ThFull).HasColumnType("numeric(18, 2)");
        entity.Property(e => e.ThR).HasDefaultValue(true);

        entity.HasOne(d => d.Block).WithMany(p => p.BlockUnits)
            .HasForeignKey(d => new { d.BlockId, d.VersionId })
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_BlockUnits_Blocks");
    }
}
