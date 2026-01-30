using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Extensions;

public static class MethodExtensions
{
    public static AttributeData? GetTestAttribute(this IMethodSymbol methodSymbol)
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

    public static AttributeData GetRequiredTestAttribute(this IMethodSymbol methodSymbol)
    {
        return GetTestAttribute(methodSymbol) ??
               throw new ArgumentException($"No test attribute found on {methodSymbol.ContainingType.Name}.{methodSymbol.Name}");
    }

    public static bool IsTest(this IMethodSymbol methodSymbol)
    {
        return methodSymbol.GetTestAttribute() != null;
    }

    public static bool IsHook(this IMethodSymbol methodSymbol, Compilation compilation)
    {
        return methodSymbol.GetAttributes().Any(x => x.IsNonGlobalHook(compilation) || x.IsGlobalHook(compilation));
    }
}
