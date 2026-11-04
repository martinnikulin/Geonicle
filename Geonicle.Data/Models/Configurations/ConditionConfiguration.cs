#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Condition на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class ConditionConfiguration : IEntityTypeConfiguration<Condition>
{
    public void Configure(EntityTypeBuilder<Condition> entity)
    {
        entity.HasKey(e => e.Id).HasName("PK_Condition_1");

        entity.ToTable("Condition", tb => tb.HasTrigger("AfterUpdateCondition"));

        entity.HasIndex(e => e.Ashnn, "IX_Condition_ash_nn");

        entity.HasIndex(e => e.Id, "IX_Condition_condition_id");

        entity.HasIndex(e => e.Ibnn, "IX_Condition_ib_nn");

        entity.HasIndex(e => e.Thnn, "IX_Condition_th_nn");

        entity.Property(e => e.Id).ValueGeneratedNever();
        entity.Property(e => e.AshCond)
            .IsRequired()
            .HasMaxLength(50);
        entity.Property(e => e.AshCondSum)
            .IsRequired()
            .HasMaxLength(50);
        entity.Property(e => e.Ashmax).HasMaxLength(50);
        entity.Property(e => e.Ashmin).HasMaxLength(50);
        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_GDCS_Condition_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_GDCS_Condition_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.IbCond)
            .IsRequired()
            .HasMaxLength(50);
        entity.Property(e => e.ThCond)
            .IsRequired()
            .HasMaxLength(50);
        entity.Property(e => e.ThCondSum)
            .IsRequired()
            .HasMaxLength(50);
        entity.Property(e => e.Thmax).HasMaxLength(50);
        entity.Property(e => e.Thmin).HasMaxLength(50);
        entity.Property(e => e.Variant1)
            .IsRequired()
            .HasMaxLength(152)
            .HasComputedColumnSql("(((([IbCond]+' ')+[ThCond])+' ')+[AshCond])", false);
        entity.Property(e => e.Variant2)
            .IsRequired()
            .HasMaxLength(150)
            .HasComputedColumnSql("(([Ibcond]+[ThCondSum])+[AshCondSum])", false);
    }
}
