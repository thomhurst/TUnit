using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Extensions;

public static class AttributeDataExtensions
{
    private static readonly string[] DataSourceAttributes =
    [
        WellKnownFullyQualifiedClassNames.ArgumentsAttribute.WithGlobalPrefix,
        WellKnownFullyQualifiedClassNames.MethodDataSourceAttribute.WithGlobalPrefix,
        WellKnownFullyQualifiedClassNames.AsyncDataSourceGeneratorAttribute.WithGlobalPrefix,
        WellKnownFullyQualifiedClassNames.AsyncUntypedDataSourceGeneratorAttribute.WithGlobalPrefix,
    ];

    public static string? GetFullyQualifiedAttributeTypeName(this AttributeData? attributeData)
    {
        return attributeData?.AttributeClass?.GloballyQualifiedNonGeneric();
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
                   x.GloballyQualified() == WellKnownFullyQualifiedClassNames.IDataAttribute.WithGlobalPrefix)
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

    public static ImmutableArray<AttributeData> ExcludingSystemAttributes(
        this IEnumerable<AttributeData> attributeDatas)
    {
        return attributeDatas
                .Where(x => x.AttributeClass?.ContainingAssembly.Name != "System.Runtime")
                .ToImmutableArray();
    }
}
