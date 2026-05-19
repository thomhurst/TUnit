using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Helpers;

internal static class AssemblyDiscoveryExclusion
{
    private const string AttributeMetadataName = "TUnit.Core.ExcludeFromTestDiscoveryAttribute";
    private const string AttributeFullyQualifiedName = "global::TUnit.Core.ExcludeFromTestDiscoveryAttribute";

    public static bool IsSelfExcluded(Compilation compilation)
    {
        return IsSelfExcluded(compilation.Assembly, compilation);
    }

    public static bool IsSelfExcluded(IAssemblySymbol assembly, Compilation compilation)
    {
        foreach (var attribute in assembly.GetAttributes())
        {
            if (IsExcludeAttribute(attribute, compilation) && !GetAssemblyMarkerTypes(attribute).Any())
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsExcludedByCurrentAssembly(IAssemblySymbol assembly, Compilation compilation)
    {
        foreach (var excludedAssemblyName in GetExcludedAssemblyNames(compilation))
        {
            if (assembly.Name == excludedAssemblyName)
            {
                return true;
            }
        }

        return false;
    }

    public static EquatableArray<string> GetExcludedAssemblyNames(Compilation compilation)
    {
        return compilation.Assembly
            .GetAttributes()
            .Where(attribute => IsExcludeAttribute(attribute, compilation))
            .SelectMany(GetAssemblyMarkerTypes)
            .Select(static type => type.ContainingAssembly.Name)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsExcludeAttribute(AttributeData attribute, Compilation compilation)
    {
        var attributeClass = attribute.AttributeClass;
        if (attributeClass is null)
        {
            return false;
        }

        var excludeAttributeType = compilation.GetTypeByMetadataName(AttributeMetadataName);
        if (excludeAttributeType is not null &&
            SymbolEqualityComparer.Default.Equals(attributeClass, excludeAttributeType))
        {
            return true;
        }

        return attributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == AttributeFullyQualifiedName ||
               attributeClass.ToDisplayString() == AttributeMetadataName;
    }

    private static IEnumerable<ITypeSymbol> GetAssemblyMarkerTypes(AttributeData attribute)
    {
        foreach (var argument in attribute.ConstructorArguments)
        {
            if (argument.Kind == TypedConstantKind.Type && argument.Value is ITypeSymbol typeSymbol)
            {
                yield return typeSymbol;
                continue;
            }

            if (argument.Kind != TypedConstantKind.Array)
            {
                continue;
            }

            foreach (var value in argument.Values)
            {
                if (value.Kind == TypedConstantKind.Type && value.Value is ITypeSymbol markerType)
                {
                    yield return markerType;
                }
            }
        }
    }
}
