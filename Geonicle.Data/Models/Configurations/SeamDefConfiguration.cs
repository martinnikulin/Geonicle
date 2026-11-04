#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности SeamDef на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class SeamDefConfiguration : IEntityTypeConfiguration<SeamDef>
{
    public void Configure(EntityTypeBuilder<SeamDef> entity)
    {
        entity.HasKey(e => e.HoleSeamId).HasName("PK_Holeseams");

        entity.ToTable(tb =>
            {
                tb.HasTrigger("AfterInsertHoleseams");
                tb.HasTrigger("AfterUpdateHoleseams");
            });

        entity.HasIndex(e => new { e.BoreholeId, e.DepthFromId, e.InterbedId }, "AX_Holeseams_1").IsUnique();

        entity.HasIndex(e => new { e.BoreholeId, e.DepthToId, e.InterbedId }, "AX_Holeseams_2").IsUnique();

        entity.HasIndex(e => e.BoreholeId, "IX_Holeseams_1");

        entity.HasIndex(e => e.SeamId, "IX_Holeseams_2");

        entity.Property(e => e.AdCoal).HasColumnType("numeric(18, 1)");
        entity.Property(e => e.AdFull).HasColumnType("numeric(18, 1)");
        entity.Property(e => e.AdR).HasDefaultValue(true);
        entity.Property(e => e.AvgCalc).HasDefaultValue(true);
        entity.Property(e => e.Comments)
            .HasMaxLength(20)
            .HasDefaultValue("");
        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_HoleSeams_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_HoleSeams_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.Dip).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.DrawFrom).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.DrawTo).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.InterbedId).HasDefaultValue(100, "DF_SeamDefs_interbed_id");
        entity.Property(e => e.Kb)
            .HasMaxLength(12)
            .HasColumnName("KB");
        entity.Property(e => e.OnMap).HasDefaultValue(true, "DF__hole_seam__onmap__29572725");
        entity.Property(e => e.ResFrom).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.ResTo).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.Stat).HasDefaultValue(true, "DF_Hole_seam_stats");
        entity.Property(e => e.ThCoal).HasColumnType("numeric(18, 2)");
        entity.Property(e => e.ThFull).HasColumnType("numeric(18, 2)");
        entity.Property(e => e.ThR).HasDefaultValue(true);

        entity.HasOne(d => d.Borehole).WithMany(p => p.SeamDefs)
            .HasForeignKey(d => d.BoreholeId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("Holes_Holeseams");

        entity.HasOne(d => d.DepthFromNavigation).WithMany(p => p.SeamDefDepthFromNavigations)
            .HasForeignKey(d => d.DepthFromId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_SeamDefs_Lithology1");

        entity.HasOne(d => d.DepthToNavigation).WithMany(p => p.SeamDefDepthToNavigations)
            .HasForeignKey(d => d.DepthToId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_SeamDefs_Lithology2");

        entity.HasOne(d => d.Seam).WithMany(p => p.SeamDefs)
            .HasForeignKey(d => d.SeamId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("Seams_Holeseams");
    }
}
