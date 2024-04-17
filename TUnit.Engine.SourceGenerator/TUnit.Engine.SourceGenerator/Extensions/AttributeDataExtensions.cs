using System;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Extensions;

public static class AttributeDataExtensions
{
    public static TestType GetTestType(this AttributeData? attributeData)
    {
        var displayString =
            attributeData?.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);

        return displayString switch
        {
            WellKnownFullyQualifiedClassNames.TestAttribute => TestType.Basic,
            WellKnownFullyQualifiedClassNames.DataDrivenTestAttribute => TestType.DataDriven,
            WellKnownFullyQualifiedClassNames.DataSourceDrivenTestAttribute => TestType.DataSourceDriven,
            WellKnownFullyQualifiedClassNames.CombinativeTestAttribute => TestType.Combinative,
            _ => throw new ArgumentException($"{displayString ?? "null"} does not map to a known test type")
        };
    }
}