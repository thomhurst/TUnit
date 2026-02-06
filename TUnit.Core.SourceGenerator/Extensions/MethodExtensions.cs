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

        return attributes
            .FirstOrDefault(x => x.AttributeClass?.BaseType?.GloballyQualified()
                                 == WellKnownFullyQualifiedClassNames.BaseTestAttribute.WithGlobalPrefix);
    }
}
