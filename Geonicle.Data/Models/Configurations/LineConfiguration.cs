#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Models.Configurations;

/// <summary>Отображение сущности Line на таблицу или представление БД: ключи, индексы, столбцы, связи, триггеры.</summary>
public class LineConfiguration : IEntityTypeConfiguration<Line>
{
    public void Configure(EntityTypeBuilder<Line> entity)
    {
        entity.HasKey(e => e.LineId).HasName("PK_Surveylines");

        entity.ToTable(tb => tb.HasTrigger("AfterUpdateSurveyLines"));

        entity.Property(e => e.DateCreated)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Surveylines_DateCreated")
            .HasColumnType("datetime");
        entity.Property(e => e.DateModified)
            .HasDefaultValueSql("(getdate())", "DF_dbo_Surveylines_DateModified")
            .HasColumnType("datetime");
        entity.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("");
        entity.Property(e => e.LineType).HasDefaultValue(LineType.РазведочнаяЛиния);
        entity.Property(e => e.Lock).HasDefaultValue(true, "DF_Surveylines_CalcSpreadAzimuths");
        entity.Property(e => e.N).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.X1).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.X2).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.Y1).HasColumnType("decimal(18, 2)");
        entity.Property(e => e.Y2).HasColumnType("decimal(18, 2)");
    }
}
