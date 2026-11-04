#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности BlockDetail на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class BlockDetailConfiguration : IEntityTypeConfiguration<BlockDetail>
{
    public void Configure(EntityTypeBuilder<BlockDetail> entity)
    {
        entity.ToTable(tb => tb.HasTrigger("AfterUpdateBlockDetails"));

        entity.HasIndex(e => e.Boundary, "AX_Blockdetails_boundary");

        entity.HasIndex(e => e.BlockId, "IX_Blockdetails");

        entity.Property(e => e.Area).HasColumnType("numeric(18, 2)");
        entity.Property(e => e.Boundary).HasDefaultValue(100, "DF_Blockdefs_boundary");
        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Blockdetails_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Blockdetails_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.Pillar).HasDefaultValue(12, "DF_Blockdetails_pillar");
        entity.Property(e => e.ResCoal).HasColumnType("numeric(18, 2)");
        entity.Property(e => e.ResFull).HasColumnType("numeric(18, 2)");
        entity.Property(e => e.Z).HasColumnType("numeric(18, 0)");

        entity.HasOne(d => d.Block).WithMany(p => p.BlockDetails)
            .HasForeignKey(d => new { d.BlockId, d.VersionId })
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_BlockDetails_Blocks");
    }
}
