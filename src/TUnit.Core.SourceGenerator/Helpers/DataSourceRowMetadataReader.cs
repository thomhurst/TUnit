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
    /// declares none of them or generated code cannot refer to one of them (the engine then falls back to
    /// reflection).
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

        // The lambda reads every setting, including the ones a usage does not set, so one getter it cannot
        // call leaves the whole attribute to reflection, which can.
        if (!CanRead(displayName, compilation) || !CanRead(skip, compilation) || !CanRead(categories, compilation))
        {
            return null;
        }

        return $"static a => ({Read(attributeClass, displayName, isArray: false)}, {Read(attributeClass, skip, isArray: false)}, {Read(attributeClass, categories, isArray: true)})";
    }

    // The most derived public property of that name, instance or static, which is the one the engine's
    // reflection fallback reads.
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

    // False for a getter generated code cannot call: an inaccessible one, or one that [Obsolete(error: true)] makes
    // a compile error no pragma suppresses. A property without a getter reads as null, as it does by reflection.
    private static bool CanRead(IPropertySymbol? property, Compilation compilation)
    {
        if (property?.GetMethod is not { } getter)
        {
            return true;
        }

        return compilation.IsSymbolAccessibleWithin(getter, compilation.Assembly) && !IsObsoleteError(property) && !IsObsoleteError(getter);

        static bool IsObsoleteError(ISymbol symbol)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass is { Name: "ObsoleteAttribute", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } }
                    && attribute.ConstructorArguments is [_, { Value: true }])
                {
                    return true;
                }
            }

            return false;
        }
    }

    // Mirrors the reflection fallback, which takes the value `as` the expected type.
    private static string Read(INamedTypeSymbol attributeClass, IPropertySymbol? property, bool isArray)
    {
        if (property?.GetMethod is null)
        {
            return "null";
        }

        var read = Access(attributeClass, property);

        var isExpectedType = isArray
            ? property.Type is IArrayTypeSymbol { Rank: 1, ElementType.SpecialType: SpecialType.System_String }
            : property.Type.SpecialType == SpecialType.System_String;

        if (isExpectedType)
        {
            return read;
        }

        // A value of another reference type (object, say) can still be the expected type at run time; a value
        // type or pointer never is.
        return property.Type.IsReferenceType || property.Type.TypeKind == TypeKind.TypeParameter
            ? $"(object?){read} as {(isArray ? "string[]" : "string")}"
            : "null";
    }

    private static string Access(INamedTypeSymbol attributeClass, IPropertySymbol property)
    {
        if (property.IsStatic)
        {
            return $"{property.ContainingType.GloballyQualified()}.{property.Name}";
        }

        // Read through the declaring type: a member of a more derived type (a field, say) can hide the property
        // from a plain member access, while reflection still finds it.
        return SymbolEqualityComparer.Default.Equals(property.ContainingType, attributeClass)
            ? $"a.{property.Name}"
            : $"(({property.ContainingType.GloballyQualified()})a).{property.Name}";
    }
}
