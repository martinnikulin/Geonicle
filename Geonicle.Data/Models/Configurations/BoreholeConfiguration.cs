#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Borehole на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class BoreholeConfiguration : IEntityTypeConfiguration<Borehole>
{
    public void Configure(EntityTypeBuilder<Borehole> entity)
    {
        entity.HasKey(e => e.BoreholeId).HasName("PK_Holes");

        entity.ToTable(tb =>
            {
                tb.HasTrigger("AfterInsertHoles");
                tb.HasTrigger("AfterUpdateHoles");
            });

        entity.Property(e => e.BoreholeId).HasComment("Код скважины");
        entity.Property(e => e.Comments).HasComment("Доп.информация по скважине");
        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_Holes_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_Holes_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.Depth)
            .HasComment("Конечная глубина")
            .HasColumnType("decimal(18, 2)");
        entity.Property(e => e.HoleName)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("Выводимое имя");
        entity.Property(e => e.HoleType).HasDefaultValue(HoleType.Скважина);
        entity.Property(e => e.OnMap).HasDefaultValue(true, "DF_Holes_onmap");
        entity.Property(e => e.X).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.Y).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.Z)
            .HasComment("Координата Z устья")
            .HasColumnType("decimal(18, 2)");
    }
}
