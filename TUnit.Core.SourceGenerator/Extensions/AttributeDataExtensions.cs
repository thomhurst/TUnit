using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Extensions;

public static class AttributeDataExtensions
{
    private static readonly string[] DataSourceAttributes =
    [
        WellKnownFullyQualifiedClassNames.ArgumentsAttribute.WithGlobalPrefix,
        WellKnownFullyQualifiedClassNames.MethodDataSourceAttribute.WithGlobalPrefix,
        WellKnownFullyQualifiedClassNames.ClassDataSourceAttribute.WithGlobalPrefix,
        WellKnownFullyQualifiedClassNames.ClassConstructorAttribute.WithGlobalPrefix,
        WellKnownFullyQualifiedClassNames.DataSourceGeneratorAttribute.WithGlobalPrefix,
    ];
    
    public static string? GetFullyQualifiedAttributeTypeName(this AttributeData? attributeData)
    {
        return attributeData?.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);
    }

    public static bool IsTest(this AttributeData? attributeData)
    {
        var displayString = attributeData?.GetFullyQualifiedAttributeTypeName();

        if (displayString == WellKnownFullyQualifiedClassNames.TestAttribute.WithGlobalPrefix)
        {
            return true;
        }

        return false;
    }
    
    public static bool IsDataSourceAttribute(this AttributeData? attributeData)
    {
        return attributeData?.AttributeClass?.AllInterfaces.Any(x =>
                   x.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) ==
                   WellKnownFullyQualifiedClassNames.IDataAttribute.WithGlobalPrefix)
               == true;
    }
    
    public static bool IsMatrixAttribute(this AttributeData? attributeData)
    {
        var displayString = attributeData?.GetFullyQualifiedAttributeTypeName();

        return WellKnownFullyQualifiedClassNames.MatrixAttribute.WithGlobalPrefix == displayString;
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