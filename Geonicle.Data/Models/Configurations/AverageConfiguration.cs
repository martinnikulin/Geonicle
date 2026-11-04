#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Average на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class AverageConfiguration : IEntityTypeConfiguration<Average>
{
    public void Configure(EntityTypeBuilder<Average> entity)
    {
        entity.HasKey(e => new { e.InterbedId, e.SeamId, e.CoalType, e.CoalGrade, e.VarId }).HasName("PK_Averages_1");

        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Averages_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Averages_DateModified")
            .HasColumnType("datetime");

        entity.HasOne(d => d.Seam).WithMany(p => p.Averages)
            .HasForeignKey(d => d.SeamId)
            .HasConstraintName("Seams_Averages");
    }
}
