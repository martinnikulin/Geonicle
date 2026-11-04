#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности HoleSeam на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class HoleSeamConfiguration : IEntityTypeConfiguration<HoleSeam>
{
    public void Configure(EntityTypeBuilder<HoleSeam> entity)
    {
        entity
            .HasNoKey()
            .ToView("HoleSeams");

        entity.Property(e => e.AdCoal).HasColumnType("numeric(18, 1)");
        entity.Property(e => e.AdFull).HasColumnType("numeric(18, 1)");
        entity.Property(e => e.Comments).HasMaxLength(20);
        entity.Property(e => e.DateCreated).HasColumnType("datetime");
        entity.Property(e => e.DateModified).HasColumnType("datetime");
        entity.Property(e => e.DepthFrom).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.DepthTo).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.Dip).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.DipFrom).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.DipTo).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.DrawFrom).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.DrawTo).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.Kb)
            .HasMaxLength(12)
            .HasColumnName("KB");
        entity.Property(e => e.ThCoal).HasColumnType("numeric(18, 2)");
        entity.Property(e => e.ThFull).HasColumnType("numeric(18, 2)");
    }
}
