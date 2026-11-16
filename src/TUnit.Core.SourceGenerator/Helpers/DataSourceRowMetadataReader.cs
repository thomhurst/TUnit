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
    /// declares none of them or generated code cannot read one (the engine then falls back to reflection).
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

        if (!IsReadable(displayName, compilation) || !IsReadable(skip, compilation) || !IsReadable(categories, compilation))
        {
            return null;
        }

        return $"static a => ({Read(displayName, "string")}, {Read(skip, "string")}, {Read(categories, "string[]")})";
    }

    // Mirrors Type.GetProperty(name): the most derived public property of that name.
    private static IPropertySymbol? FindProperty(INamedTypeSymbol attributeClass, string name)
    {
        for (var type = attributeClass; type is not null; type = type.BaseType)
        {
            foreach (var member in type.GetMembers(name))
            {
                if (member is IPropertySymbol { DeclaredAccessibility: Accessibility.Public, IsIndexer: false } property
                    && (!property.IsStatic || SymbolEqualityComparer.Default.Equals(type, attributeClass)))
                {
                    return property;
                }
            }
        }

        return null;
    }

    private static bool IsReadable(IPropertySymbol? property, Compilation compilation)
    {
        return property is null
            || property.GetMethod is { } getter && compilation.IsSymbolAccessibleWithin(getter, compilation.Assembly);
    }

    private static string Read(IPropertySymbol? property, string type)
    {
        if (property is null)
        {
            return "null";
        }

        var access = property.IsStatic
            ? $"{property.ContainingType.GloballyQualified()}.{property.Name}"
            : $"a.{property.Name}";

        var isExpectedType = type == "string"
            ? property.Type.SpecialType == SpecialType.System_String
            : property.Type is IArrayTypeSymbol { Rank: 1, ElementType.SpecialType: SpecialType.System_String };

        // Reflection reads the value as an object and keeps it only when it is the expected type.
        return isExpectedType ? access : $"(object?){access} as {type}";
    }
}
