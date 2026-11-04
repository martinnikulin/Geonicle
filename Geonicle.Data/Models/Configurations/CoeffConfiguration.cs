#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Coeff на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class CoeffConfiguration : IEntityTypeConfiguration<Coeff>
{
    public void Configure(EntityTypeBuilder<Coeff> entity)
    {
        entity.HasKey(e => new { e.SeamId, e.InterbedId, e.CoalGrade, e.CoalType });

        entity.HasIndex(e => e.SeamId, "IX_Coeffs");

        entity.HasIndex(e => e.InterbedId, "IX_Coeffs_1");

        entity.HasIndex(e => e.CoalGrade, "IX_Coeffs_2");

        entity.HasIndex(e => e.CoalType, "IX_Coeffs_3");

        entity.Property(e => e.A).HasColumnType("decimal(18, 5)");
        entity.Property(e => e.B).HasColumnType("decimal(18, 5)");
        entity.Property(e => e.C).HasColumnType("decimal(18, 5)");
        entity.Property(e => e.CoeffId).ValueGeneratedOnAdd();
        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Coeffs_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Coeffs_DateModified")
            .HasColumnType("datetime");

        entity.HasOne(d => d.Seam).WithMany(p => p.Coeffs)
            .HasForeignKey(d => d.SeamId)
            .HasConstraintName("Seams_Coeffs");
    }
}
