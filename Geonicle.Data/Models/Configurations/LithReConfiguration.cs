#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности LithRe на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class LithReConfiguration : IEntityTypeConfiguration<LithRe>
{
    public void Configure(EntityTypeBuilder<LithRe> entity)
    {
        entity.HasKey(e => new { e.LithologyId, e.InterbedId }).HasName("PK_Lithres");

        entity.HasIndex(e => e.Reserves, "IX_Lithology_1");

        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Lithres_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Lithres_DateModified")
            .HasColumnType("datetime");

        entity.HasOne(d => d.Lithology).WithMany(p => p.LithRes)
            .HasForeignKey(d => d.LithologyId)
            .HasConstraintName("Lithology_Lithres");
    }
}
