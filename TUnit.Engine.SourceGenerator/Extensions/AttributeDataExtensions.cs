using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.Extensions;

public static class AttributeDataExtensions
{
    private static readonly string[] DataSourceAttributes =
    [
        WellKnownFullyQualifiedClassNames.ArgumentsAttribute.WithGlobalPrefix,
        WellKnownFullyQualifiedClassNames.MethodDataSourceAttribute.WithGlobalPrefix,
        WellKnownFullyQualifiedClassNames.EnumerableMethodDataAttribute.WithGlobalPrefix,
        WellKnownFullyQualifiedClassNames.ClassDataSourceAttribute.WithGlobalPrefix,
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
        var displayString = attributeData?.GetFullyQualifiedAttributeTypeName();

        return DataSourceAttributes.Any(x => x == displayString);
    }
    
    public static bool IsMatrixAttribute(this AttributeData? attributeData)
    {
        var displayString = attributeData?.GetFullyQualifiedAttributeTypeName();

        return WellKnownFullyQualifiedClassNames.MatrixAttribute.WithGlobalPrefix == displayString;
    }
    
    public static bool IsNonGlobalHook(this AttributeData attributeData)
    {
        var displayString = attributeData.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);
        return displayString == WellKnownFullyQualifiedClassNames.BeforeAttribute.WithGlobalPrefix ||
               displayString == WellKnownFullyQualifiedClassNames.AfterAttribute.WithGlobalPrefix;
    }
    
    public static bool IsGlobalHook(this AttributeData attributeData)
    {
        var displayString = attributeData.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);
        return displayString == WellKnownFullyQualifiedClassNames.BeforeEveryAttribute.WithGlobalPrefix ||
               displayString == WellKnownFullyQualifiedClassNames.AfterEveryAttribute.WithGlobalPrefix;
    }
}