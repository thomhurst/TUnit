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
    /// declares none of them or generated code cannot express one of the reads (the engine then falls back to reflection).
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

        if (!CanEmitRead(displayName, compilation) || !CanEmitRead(skip, compilation) || !CanEmitRead(categories, compilation))
        {
            return null;
        }

        return $"static a => ({Read(attributeClass, displayName, "string")}, {Read(attributeClass, skip, "string")}, {Read(attributeClass, categories, "string[]")})";
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

    // False only when reflection could read a value that generated code cannot express: a getter generated code
    // cannot call, or a type that cannot be converted to object. A property without a getter reads as null,
    // as the reflection fallback does.
    private static bool CanEmitRead(IPropertySymbol? property, Compilation compilation)
    {
        if (property?.GetMethod is not { } getter)
        {
            return true;
        }

        return compilation.IsSymbolAccessibleWithin(getter, compilation.Assembly)
            && property.Type is { IsRefLikeType: false, TypeKind: not (TypeKind.Pointer or TypeKind.FunctionPointer) };
    }

    private static string Read(INamedTypeSymbol attributeClass, IPropertySymbol? property, string type)
    {
        if (property?.GetMethod is null)
        {
            return "null";
        }

        // Read through the declaring type: a member of a more derived type (a field, say) can hide the property
        // from a plain member access, while reflection still finds it.
        var access = property.IsStatic
            ? $"{property.ContainingType.GloballyQualified()}.{property.Name}"
            : SymbolEqualityComparer.Default.Equals(property.ContainingType, attributeClass)
                ? $"a.{property.Name}"
                : $"(({property.ContainingType.GloballyQualified()})a).{property.Name}";

        var isExpectedType = type == "string"
            ? property.Type.SpecialType == SpecialType.System_String
            : property.Type is IArrayTypeSymbol { Rank: 1, ElementType.SpecialType: SpecialType.System_String };

        // Reflection reads the value as an object and keeps it only when it is the expected type.
        return isExpectedType ? access : $"(object?){access} as {type}";
    }
}
