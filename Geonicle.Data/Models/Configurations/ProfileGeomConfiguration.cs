#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности ProfileGeom на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class ProfileGeomConfiguration : IEntityTypeConfiguration<ProfileGeom>
{
    public void Configure(EntityTypeBuilder<ProfileGeom> entity)
    {
        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Geoms_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Geoms_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.FeatureName).HasMaxLength(100);
        entity.Property(e => e.FeatureSubType).HasDefaultValue(0);
        entity.Property(e => e.InterbedId).HasDefaultValue(0);
        entity.Property(e => e.ObjectId)
            .IsRequired()
            .HasMaxLength(10)
            .IsFixedLength();

        entity.HasOne(d => d.Line).WithMany(p => p.ProfileGeoms)
            .HasForeignKey(d => d.LineId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Geoms_Surveylines");
    }
}
