using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Helpers;

/// <summary>
/// Builds the typed read of a custom data source attribute's <c>DisplayName</c>, <c>Skip</c> and
/// <c>Categories</c> properties, which the engine otherwise looks up by name with reflection.
/// </summary>
internal static class DataSourceRowMetadataReader
{
    /// <summary>
    /// Returns a lambda reading the row settings of <paramref name="attributeClass"/>, or <c>null</c> when it
    /// declares none of them or one of them is not a plain typed read (the engine then falls back to reflection).
    /// </summary>
    public static string? TryCreate(INamedTypeSymbol attributeClass, Compilation compilation)
    {
        var displayName = FindProperty(attributeClass, "DisplayName");
        var skip = FindProperty(attributeClass, "Skip");
        var categories = FindProperty(attributeClass, "Categories");

        if (displayName is null && skip is null && categories is null)
        {
            return null;
        }

        if (!IsPlainRead(displayName, isArray: false, compilation)
            || !IsPlainRead(skip, isArray: false, compilation)
            || !IsPlainRead(categories, isArray: true, compilation))
        {
            return null;
        }

        return $"static a => ({Read(attributeClass, displayName)}, {Read(attributeClass, skip)}, {Read(attributeClass, categories)})";
    }

    // The most derived public property of that name, which is the one Type.GetProperty(name) reads.
    private static IPropertySymbol? FindProperty(INamedTypeSymbol attributeClass, string name)
    {
        for (var type = attributeClass; type is not null; type = type.BaseType)
        {
            foreach (var member in type.GetMembers(name))
            {
                if (member is IPropertySymbol { DeclaredAccessibility: Accessibility.Public, IsIndexer: false } property)
                {
                    return property;
                }
            }
        }

        return null;
    }

    // False for anything reflection handles in its own way: a static property, a property of another type, or
    // a getter generated code cannot call. A property without a getter reads as null, as the reflection
    // fallback does.
    private static bool IsPlainRead(IPropertySymbol? property, bool isArray, Compilation compilation)
    {
        if (property?.GetMethod is not { } getter)
        {
            return true;
        }

        var isExpectedType = isArray
            ? property.Type is IArrayTypeSymbol { Rank: 1, ElementType.SpecialType: SpecialType.System_String }
            : property.Type.SpecialType == SpecialType.System_String;

        return !property.IsStatic && isExpectedType && compilation.IsSymbolAccessibleWithin(getter, compilation.Assembly);
    }

    private static string Read(INamedTypeSymbol attributeClass, IPropertySymbol? property)
    {
        if (property?.GetMethod is null)
        {
            return "null";
        }

        // Read through the declaring type: a member of a more derived type (a field, say) can hide the property
        // from a plain member access, while reflection still finds it.
        return SymbolEqualityComparer.Default.Equals(property.ContainingType, attributeClass)
            ? $"a.{property.Name}"
            : $"(({property.ContainingType.GloballyQualified()})a).{property.Name}";
    }
}
