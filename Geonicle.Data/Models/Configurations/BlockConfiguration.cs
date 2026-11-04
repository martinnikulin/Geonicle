#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Block на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> entity)
    {
        entity.HasKey(e => new { e.BlockId, e.VersionId });

        entity.ToTable(tb => tb.HasTrigger("AfterUpdateBlocks"));

        entity.HasIndex(e => e.PartId, "IX_Blocks");

        entity.Property(e => e.BlockId).ValueGeneratedOnAdd();
        entity.Property(e => e.VersionId).HasDefaultValue(1, "DF_Blocks_VersionId");
        entity.Property(e => e.AdC).HasColumnType("numeric(18, 2)");
        entity.Property(e => e.AdCoal).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.AdF).HasColumnType("numeric(18, 2)");
        entity.Property(e => e.AdFull).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.Balance).HasDefaultValue(true, "DF_Block_balance");
        entity.Property(e => e.BlockName)
            .IsRequired()
            .HasMaxLength(12)
            .IsFixedLength();
        entity.Property(e => e.CoalType).HasDefaultValue(5);
        entity.Property(e => e.Comments).HasColumnType("ntext");
        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Blocks_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Blocks_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.Dip)
            .HasDefaultValue(0m, "DF_Block_dip")
            .HasColumnType("decimal(18, 1)");
        entity.Property(e => e.ThC).HasColumnType("numeric(18, 2)");
        entity.Property(e => e.ThCoal).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.ThF).HasColumnType("numeric(18, 2)");
        entity.Property(e => e.ThFull).HasColumnType("decimal(18, 2)");

        entity.HasOne(d => d.Condition).WithMany(p => p.Blocks)
            .HasForeignKey(d => d.ConditionId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("Blocks_Condition");

        entity.HasOne(d => d.Part).WithMany(p => p.Blocks)
            .HasForeignKey(d => d.PartId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("Blocks_Parts");

        entity.HasOne(d => d.Version).WithMany(p => p.Blocks)
            .HasForeignKey(d => d.VersionId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Blocks_Versions");
    }
}
