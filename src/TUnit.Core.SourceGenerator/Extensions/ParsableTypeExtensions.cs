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
    /// convert from string through the language (built-ins or user-defined operators) or through
    /// <see cref="IsParsableFromString"/> are excluded, so those keep their existing conversion.
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

        if (type is null || type.SpecialType != SpecialType.None || ContainsTypeParameters(type)
            || type.IsParsableFromString()
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
                string typeName => ResolveTypeName(typeName, compilation),
                _ => null,
            };

            if (converter is null)
            {
                // The nearest attribute wins, as with TypeDescriptor; an unresolved one is unsupported.
                return false;
            }
        }

        if (converter is null || converter.IsAbstract || converter.IsUnboundGenericType || ContainsTypeParameters(converter)
            || IsObsoleteAsError(converter)
            || !compilation.IsSymbolAccessibleWithin(converter, compilation.Assembly)
            || !OverridesConvertFrom(converter))
        {
            return false;
        }

        var constructors = converter.InstanceConstructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public && !IsObsoleteAsError(c))
            .ToArray();

        // TypeDescriptor prefers a constructor that receives the target type.
        if (constructors.Any(c => c.Parameters is [{ RefKind: RefKind.None, Type: var p }] && p.ToDisplayString() == "System.Type"))
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

    /// <summary>
    /// Resolves <c>"Namespace.Type, Assembly"</c> within the named assembly, or the compilation when no assembly is given.
    /// Names with generic arguments are not supported.
    /// </summary>
    private static INamedTypeSymbol? ResolveTypeName(string typeName, Compilation compilation)
    {
        if (typeName.IndexOf('[') >= 0)
        {
            return null;
        }

        var parts = typeName.Split(',');
        var metadataName = parts[0].Trim();
        if (parts.Length == 1)
        {
            return compilation.GetTypeByMetadataName(metadataName);
        }

        var assemblyName = parts[1].Trim();
        if (compilation.Assembly.Identity.Name == assemblyName)
        {
            return compilation.Assembly.GetTypeByMetadataName(metadataName);
        }

        return compilation.SourceModule.ReferencedAssemblySymbols
            .FirstOrDefault(a => a.Identity.Name == assemblyName)
            ?.GetTypeByMetadataName(metadataName);
    }

    /// <summary>
    /// The base <c>TypeConverter.ConvertFrom</c> always throws, so a converter that never overrides it cannot convert.
    /// </summary>
    private static bool OverridesConvertFrom(INamedTypeSymbol converter)
    {
        var overridden = false;
        for (var current = converter; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == "System.ComponentModel.TypeConverter")
            {
                return overridden;
            }

            overridden |= current.GetMembers("ConvertFrom").OfType<IMethodSymbol>().Any(m => m.IsOverride);
        }

        return false;
    }

    private static bool ContainsTypeParameters(ITypeSymbol type) => type switch
    {
        ITypeParameterSymbol => true,
        IArrayTypeSymbol array => ContainsTypeParameters(array.ElementType),
        INamedTypeSymbol named => named.TypeArguments.Any(ContainsTypeParameters)
                                  || (named.ContainingType is { } containing && ContainsTypeParameters(containing)),
        _ => false,
    };

    private static bool IsObsoleteAsError(ISymbol symbol)
        => symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "System.ObsoleteAttribute"
                                           && a.ConstructorArguments is [_, { Value: true }]);
}
