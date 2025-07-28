using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Extensions;

public static class AttributeDataExtensions
{
    public static string? GetFullyQualifiedAttributeTypeName(this AttributeData? attributeData)
    {
        return attributeData?.AttributeClass?.GloballyQualifiedNonGeneric();
    }

    public static bool IsTestAttribute(this AttributeData? attributeData)
    {
        return attributeData?.AttributeClass?.AllInterfaces.Any(x =>
                x.GloballyQualified() == WellKnownFullyQualifiedClassNames.TestAttribute.WithGlobalPrefix)
            == true;
    }

    public static bool IsDataSourceAttribute(this AttributeData? attributeData)
    {
        return attributeData?.AttributeClass?.AllInterfaces.Any(x =>
                   x.GloballyQualified() == WellKnownFullyQualifiedClassNames.IDataSourceAttribute.WithGlobalPrefix)
               == true;
    }

    public static bool IsNonGlobalHook(this AttributeData attributeData, Compilation compilation)
    {
        return SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass,
                   compilation.GetTypeByMetadataName(WellKnownFullyQualifiedClassNames.BeforeAttribute
                       .WithoutGlobalPrefix))
               || SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass,
                   compilation.GetTypeByMetadataName(WellKnownFullyQualifiedClassNames.AfterAttribute
                       .WithoutGlobalPrefix));
    }

    public static bool IsGlobalHook(this AttributeData attributeData, Compilation compilation)
    {
        return SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass,
                   compilation.GetTypeByMetadataName(WellKnownFullyQualifiedClassNames.BeforeEveryAttribute
                       .WithoutGlobalPrefix))
               || SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass,
                   compilation.GetTypeByMetadataName(WellKnownFullyQualifiedClassNames.AfterEveryAttribute
                       .WithoutGlobalPrefix));
    }
}
