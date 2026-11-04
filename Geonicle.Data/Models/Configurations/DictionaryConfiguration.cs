#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Dictionary на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class DictionaryConfiguration : IEntityTypeConfiguration<Dictionary>
{
    public void Configure(EntityTypeBuilder<Dictionary> entity)
    {
        entity.ToTable("Dictionary");

        entity.HasIndex(e => new { e.Section, e.N }, "IX_Dictionary_nn").IsUnique();

        entity.Property(e => e.Actual).HasDefaultValue(true);
        entity.Property(e => e.Bdcode).HasColumnName("BDCode");
        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Dictionary_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Dictionary_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(500);
        entity.Property(e => e.FullDesc).HasMaxLength(500);
        entity.Property(e => e.Section)
            .IsRequired()
            .HasMaxLength(255);
        entity.Property(e => e.Type)
            .HasMaxLength(5)
            .IsFixedLength()
            .HasDefaultValue("var", "DF_Dictionary_dicttype");
    }
}
