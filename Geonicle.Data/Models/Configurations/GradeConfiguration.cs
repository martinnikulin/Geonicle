#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Grade на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> entity)
    {
        entity.ToTable("Grade");

        entity.Property(e => e.GradeId)
            .HasMaxLength(10)
            .IsFixedLength();
        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Grade_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Grade_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.Description).HasMaxLength(100);
        entity.Property(e => e.GradeName)
            .HasMaxLength(10)
            .IsFixedLength();

        entity.HasOne(d => d.Dictionary).WithMany(p => p.Grades)
            .HasForeignKey(d => d.DictionaryId)
            .HasConstraintName("Dictionary_Grade");
    }
}
