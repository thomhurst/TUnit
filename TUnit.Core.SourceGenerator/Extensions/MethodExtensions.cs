using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Extensions;

public static class MethodExtensions
{
    public static AttributeData GetRequiredTestAttribute(this IMethodSymbol methodSymbol)
    {
        return GetTestAttribute(methodSymbol) ??
               throw new ArgumentException($"No test attribute found on {methodSymbol.ContainingType.Name}.{methodSymbol.Name}");
    }

    private static AttributeData? GetTestAttribute(IMethodSymbol methodSymbol)
    {
        var attributes = methodSymbol.GetAttributes();

        if (attributes.IsDefaultOrEmpty)
        {
            return null;
        }

        var baseTestAttribute = WellKnownFullyQualifiedClassNames.BaseTestAttribute.WithGlobalPrefix;

        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass?.BaseType?.GloballyQualified() == baseTestAttribute)
            {
                return attribute;
            }
        }

        return null;
    }
}
