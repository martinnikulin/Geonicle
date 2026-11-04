#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности AssayVarsCr на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class AssayVarsCrConfiguration : IEntityTypeConfiguration<AssayVarsCr>
{
    public void Configure(EntityTypeBuilder<AssayVarsCr> entity)
    {
        entity.ToTable("AssayVarsCR");

        entity.Property(e => e.CoalTypeId).HasDefaultValue(0, "DF_AssayVarsCR_CoalTypeId");
        entity.Property(e => e.N).HasDefaultValue(1, "DF_AssayVarsCR_N");

        entity.HasOne(d => d.CoalRank).WithMany(p => p.AssayVarsCrs)
            .HasForeignKey(d => d.CoalRankId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_AssayVarsCR_Dictionary");
    }
}
