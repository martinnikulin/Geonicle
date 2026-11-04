#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Lithology на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class LithologyConfiguration : IEntityTypeConfiguration<Lithology>
{
    public void Configure(EntityTypeBuilder<Lithology> entity)
    {
        entity.ToTable("Lithology", tb =>
            {
                tb.HasTrigger("AfterInsertLithology");
                tb.HasTrigger("AfterUpdateLithology");
            });

        entity.HasIndex(e => new { e.BoreholeId, e.DepthFrom }, "AX_lithology_1").IsUnique();

        entity.HasIndex(e => new { e.BoreholeId, e.DepthTo }, "AX_lithology_2").IsUnique();

        entity.HasIndex(e => e.BoreholeId, "IX_Lithology_1");

        entity.HasIndex(e => e.RockId, "IX_Lithology_2");

        entity.Property(e => e.Angle).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.Core)
            .HasDefaultValue(0m, "DF_Lithology_Core")
            .HasColumnType("decimal(18, 2)");
        entity.Property(e => e.CoreNorm)
            .HasDefaultValue(0m, "DF_Lithology_CoreNorm")
            .HasColumnType("decimal(18, 2)");
        entity.Property(e => e.CoreProc)
            .HasDefaultValue(0m, "DF_Lithology_CoreProc")
            .HasColumnType("decimal(18, 0)");
        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_Lithology_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_Lithology_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.DepthFrom).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.DepthTo).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.Description).HasColumnType("ntext");
        entity.Property(e => e.Formation).HasDefaultValue(0, "DF_Lithology_svita");
        entity.Property(e => e.Th).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.ThNorm).HasColumnType("decimal(18, 2)");

        entity.HasOne(d => d.Borehole).WithMany(p => p.Lithologies)
            .HasForeignKey(d => d.BoreholeId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("Holes_Lithology");
    }
}
