#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Drawing на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class DrawingConfiguration : IEntityTypeConfiguration<Drawing>
{
    public void Configure(EntityTypeBuilder<Drawing> entity)
    {
        entity.Property(e => e.DrawingId)
            .HasMaxLength(32)
            .IsFixedLength();
        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Drawings_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Drawings_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.FileName).IsRequired();
        entity.Property(e => e.Gdbid).HasColumnName("GDBId");
    }
}
