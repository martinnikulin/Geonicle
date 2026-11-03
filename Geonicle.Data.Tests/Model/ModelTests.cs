using System.Collections;
using System.Collections.ObjectModel;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Geonicle.Data.Tests.Model;

/// <summary>Настройки модели EF, от которых зависит поведение приложения. Ловят поломки при правках моделей.</summary>
public class ModelTests
{
    private static IModel Model()
    {
        using var db = new TestDb().CreateDbContext();
        return db.Model;
    }

    [Fact]
    public void Model_builds() => Assert.NotEmpty(Model().GetEntityTypes());

    [Fact]
    public void Coeff_key_is_the_identity_column()
    {
        var key = Model().FindEntityType(typeof(Coeff))!.FindPrimaryKey()!;
        Assert.Equal(new[] { nameof(Coeff.CoeffId) }, key.Properties.Select(p => p.Name));
    }

    [Fact]
    public void Part_description_is_nullable() =>
        Assert.True(Model().FindEntityType(typeof(Part))!.FindProperty(nameof(Part.Description))!.IsNullable);

    [Fact]
    public void Deleting_a_lithology_layer_never_cascades_to_seam_intersections()
    {
        var keys = Model().FindEntityType(typeof(SeamDef))!.GetForeignKeys()
            .Where(fk => fk.PrincipalEntityType.ClrType == typeof(Lithology))
            .ToList();

        Assert.Equal(2, keys.Count);
        Assert.All(keys, fk => Assert.Equal(DeleteBehavior.NoAction, fk.DeleteBehavior));
    }

    [Fact]
    public void Required_relationships_never_use_ClientSetNull()
    {
        var offenders = Model().GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys())
            .Where(fk => fk.IsRequired && fk.DeleteBehavior == DeleteBehavior.ClientSetNull)
            .Select(fk => $"{fk.DeclaringEntityType.ClrType.Name} -> {fk.PrincipalEntityType.ClrType.Name}")
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void All_dependent_collections_in_models_are_observable()
    {
        var offenders = new List<string>();
        foreach (var type in typeof(GDB52Context).Assembly.GetTypes()
                     .Where(t => t.Namespace == "Data.Models" && t.IsClass && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null))
        {
            var instance = Activator.CreateInstance(type)!;
            foreach (var property in type.GetProperties().Where(IsCollectionNavigation))
            {
                var value = property.GetValue(instance);
                if (value == null || !IsObservable(value))
                    offenders.Add($"{type.Name}.{property.Name}");
            }
        }

        Assert.Empty(offenders);
    }

    private static bool IsCollectionNavigation(System.Reflection.PropertyInfo property) =>
        property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>);

    private static bool IsObservable(object value) =>
        value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(ObservableCollection<>);
}
