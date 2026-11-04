using Data.Models;
using Geonicle.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Geonicle.Data.Tests.Infrastructure;

/// <summary>Хранение КБ: в БД строка (подпись [Display] или имя члена), в коде — KbType.</summary>
public class DisplayNameEnumConverterTests
{
    [Theory]
    [InlineData(KbType.Б, "Б")]
    [InlineData(KbType.БезКерна, "Б(без керна)")]
    [InlineData(KbType.К, "К")]
    [InlineData(KbType.КБ, "К+Б")]
    [InlineData(KbType.БК, "Б(К)")]
    [InlineData(KbType.БНК, "Б(НК)")]
    [InlineData(KbType.НетДанных, "Нет данных")]
    public void Every_member_is_stored_as_the_text_the_database_already_uses(KbType value, string text)
    {
        Assert.Equal(text, DisplayNameEnumConverter<KbType>.Write(value));
        Assert.Equal(value, DisplayNameEnumConverter<KbType>.Read(text));
    }

    [Fact]
    public void All_values_fit_the_12_character_column()
    {
        foreach (var value in Enum.GetValues<KbType>())
            Assert.True(DisplayNameEnumConverter<KbType>.Write(value).Length <= 12, value.ToString());
    }

    [Theory]
    [InlineData("X")]
    [InlineData("")]
    [InlineData("б")]
    public void Unknown_text_is_an_error_not_a_silent_value(string text)
    {
        var error = Assert.Throws<InvalidOperationException>(() => DisplayNameEnumConverter<KbType>.Read(text));
        Assert.Contains("Неизвестное значение", error.Message);
        Assert.Contains("К+Б", error.Message);
    }

    [Fact]
    public void Surrounding_spaces_in_the_database_text_are_ignored()
    {
        Assert.Equal(KbType.КБ, DisplayNameEnumConverter<KbType>.Read(" К+Б "));
    }

    [Fact]
    public void Seam_intersection_keeps_Kb_through_the_context_and_null_stays_null()
    {
        var testDb = new TestDb();
        using (var db = testDb.CreateDbContext())
        {
            db.SeamDefs.AddRange(
                new SeamDef { BoreholeId = 1, SeamId = 1, DepthFromId = 1, DepthToId = 1, InterbedId = 100, Kb = KbType.КБ },
                new SeamDef { BoreholeId = 1, SeamId = 1, DepthFromId = 2, DepthToId = 2, InterbedId = 100, Kb = null });
            db.SaveChanges();
        }

        using var read = testDb.CreateDbContext();
        Assert.Equal(KbType.КБ, read.SeamDefs.Single(s => s.DepthFromId == 1).Kb);
        Assert.Null(read.SeamDefs.Single(s => s.DepthFromId == 2).Kb);
    }

    [Fact]
    public void Model_uses_the_converter_for_both_the_table_and_the_view()
    {
        using var db = new TestDb().CreateDbContext();
        foreach (var type in new[] { typeof(SeamDef), typeof(HoleSeam) })
        {
            var property = db.Model.FindEntityType(type)!.FindProperty("Kb")!;
            Assert.Equal(typeof(KbType?), property.ClrType);
            Assert.IsType<DisplayNameEnumConverter<KbType>>(property.GetValueConverter());
            Assert.Equal(12, property.GetMaxLength());
        }
    }
}
