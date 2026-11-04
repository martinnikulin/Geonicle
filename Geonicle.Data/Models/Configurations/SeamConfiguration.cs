#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Seam на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class SeamConfiguration : IEntityTypeConfiguration<Seam>
{
    public void Configure(EntityTypeBuilder<Seam> entity)
    {
        entity.ToTable(tb =>
            {
                tb.HasTrigger("AfterInsertSeams");
                tb.HasTrigger("AfterUpdateSeams");
            });

        entity.Property(e => e.SeamId).HasComment("Код пласта");
        entity.Property(e => e.Actual).HasDefaultValue(true, "DF_Seam_aсtual");
        entity.Property(e => e.AdBed).HasColumnType("numeric(18, 2)");
        entity.Property(e => e.AdRoof).HasColumnType("numeric(18, 2)");
        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Seams_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Seams_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.DdaBed).HasColumnType("numeric(18, 2)");
        entity.Property(e => e.DdaRoof).HasColumnType("numeric(18, 2)");
        entity.Property(e => e.Description)
            .HasMaxLength(50)
            .HasComment("Название пласта");
        entity.Property(e => e.GroupSeamId).HasMaxLength(12);
        entity.Property(e => e.ShortName)
            .HasMaxLength(50)
            .HasComment("Короткое название");
        entity.Property(e => e.Strata).HasComment("Стратиграфия");
    }
}
