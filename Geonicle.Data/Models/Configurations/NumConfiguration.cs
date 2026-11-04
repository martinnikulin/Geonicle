#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Num на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class NumConfiguration : IEntityTypeConfiguration<Num>
{
    public void Configure(EntityTypeBuilder<Num> entity)
    {
        entity.HasKey(e => e.N).HasName("PK__Nums__3BD0199378E9C54B");

        entity.Property(e => e.N)
            .ValueGeneratedNever()
            .HasColumnName("n");
        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Nums_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Nums_DateModified")
            .HasColumnType("datetime");
    }
}
