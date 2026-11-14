using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TUnit.Analyzers.Extensions;

internal static class ParsableTypeExtensions
{
    public static bool IsParsableFromString(this ITypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        // Unwrap nullable value types (e.g., DateTime? -> DateTime)
        if (type is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } nullableType)
        {
            type = nullableType.TypeArguments[0];
        }

        if (type.AllInterfaces.Any(i =>
                i is { IsGenericType: true, MetadataName: "IParsable`1" }
                && i.ContainingNamespace?.ToDisplayString() == "System"
                && SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], type)))
        {
            return true;
        }

        // Fallback for older TFMs where IParsable doesn't exist
        if (type.SpecialType == SpecialType.System_DateTime)
        {
            return true;
        }

        var fullyQualifiedName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return fullyQualifiedName is
            "global::System.DateTimeOffset" or
            "global::System.TimeSpan" or
            "global::System.Guid" or
            "global::System.DateOnly" or
            "global::System.TimeOnly";
    }

    /// <summary>
    /// Finds the <c>[TypeConverter]</c> that converts a string argument to <paramref name="type"/>, mirroring
    /// how <c>TypeDescriptor</c> picks a converter declared on the type or a base type. Types that already
    /// convert from string through the language (built-ins or user-defined operators) are excluded.
    /// </summary>
    public static bool TryGetStringTypeConverter(this ITypeSymbol? type, Compilation compilation,
        out INamedTypeSymbol converterType, out bool passTargetType)
    {
        converterType = null!;
        passTargetType = false;

        if (type is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } nullableType)
        {
            type = nullableType.TypeArguments[0];
        }

        if (type is null or ITypeParameterSymbol || type.SpecialType != SpecialType.None
            || (compilation is CSharpCompilation csharp && csharp.ClassifyConversion(csharp.GetSpecialType(SpecialType.System_String), type).Exists))
        {
            return false;
        }

        INamedTypeSymbol? converter = null;
        for (var current = type; current is not null && converter is null; current = current.BaseType)
        {
            var attribute = current.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString() == "System.ComponentModel.TypeConverterAttribute"
                && a.ConstructorArguments.Length == 1);

            if (attribute is null)
            {
                continue;
            }

            converter = attribute.ConstructorArguments[0].Value switch
            {
                INamedTypeSymbol t => t,
                string typeName when typeName.IndexOf('[') < 0 => compilation.GetTypeByMetadataName(typeName.Split(',')[0].Trim()),
                _ => null,
            };

            if (converter is null)
            {
                // The nearest attribute wins, as with TypeDescriptor; an unresolved one is unsupported.
                return false;
            }
        }

        if (converter is null || converter.IsAbstract || converter.IsUnboundGenericType
            || !compilation.IsSymbolAccessibleWithin(converter, compilation.Assembly)
            || !InheritsFrom(converter, "System.ComponentModel.TypeConverter"))
        {
            return false;
        }

        var constructors = converter.InstanceConstructors.Where(c => c.DeclaredAccessibility == Accessibility.Public).ToArray();

        // TypeDescriptor prefers a constructor that receives the target type.
        if (constructors.Any(c => c.Parameters is [{ Type: var p }] && p.ToDisplayString() == "System.Type"))
        {
            passTargetType = true;
        }
        else if (!constructors.Any(c => c.Parameters.Length == 0))
        {
            return false;
        }

        converterType = converter;
        return true;
    }

    private static bool InheritsFrom(INamedTypeSymbol type, string baseTypeName)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == baseTypeName)
            {
                return true;
            }
        }

        return false;
    }
}
