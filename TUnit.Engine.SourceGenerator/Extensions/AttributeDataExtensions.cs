using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Extensions;

public static class AttributeDataExtensions
{
    public static string? GetFullyQualifiedAttributeTypeName(this AttributeData? attributeData)
    {
        return attributeData?.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);
    }

    public static TestType GetTestType(this AttributeData? attributeData)
    {
        var displayString = attributeData?.GetFullyQualifiedAttributeTypeName();

        if (displayString == WellKnownFullyQualifiedClassNames.TestAttribute.WithGlobalPrefix)
        {
            return TestType.Basic;
        }

        if (displayString == WellKnownFullyQualifiedClassNames.DataDrivenTestAttribute.WithGlobalPrefix)
        {
            return TestType.DataDriven;
        }

        if (displayString == WellKnownFullyQualifiedClassNames.DataSourceDrivenTestAttribute.WithGlobalPrefix)
        {
            return TestType.DataSourceDriven;
        }

        if (displayString == WellKnownFullyQualifiedClassNames.CombinativeTestAttribute.WithGlobalPrefix)
        {
            return TestType.Combinative;
        }

        throw new ArgumentException($"{displayString ?? "null"} does not map to a known test type");
    }
}