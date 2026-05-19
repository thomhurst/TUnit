using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Helpers;

internal static class AssemblyDiscoveryExclusion
{
    private const string AttributeMetadataName = "TUnit.Core.ExcludeFromTestDiscoveryAttribute";
    private const string AttributeFullyQualifiedName = "global::TUnit.Core.ExcludeFromTestDiscoveryAttribute";

    public static bool IsExcluded(Compilation compilation)
    {
        return IsExcluded(compilation.Assembly, compilation);
    }

    public static bool IsExcluded(IAssemblySymbol assembly, Compilation compilation)
    {
        var excludeAttributeType = compilation.GetTypeByMetadataName(AttributeMetadataName);

        foreach (var attribute in assembly.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass is null)
            {
                continue;
            }

            if (excludeAttributeType is not null &&
                SymbolEqualityComparer.Default.Equals(attributeClass, excludeAttributeType))
            {
                return true;
            }

            if (attributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == AttributeFullyQualifiedName ||
                attributeClass.ToDisplayString() == AttributeMetadataName)
            {
                return true;
            }
        }

        return false;
    }
}
