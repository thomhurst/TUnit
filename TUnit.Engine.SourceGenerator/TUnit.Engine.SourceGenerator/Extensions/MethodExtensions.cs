using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.Extensions;

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
            .FirstOrDefault(x => x.AttributeClass?.BaseType?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) 
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

    public static AttributeData[] GetAttributesIncludingClass(this IMethodSymbol methodSymbol, INamedTypeSymbol namedTypeSymbol)
    {
        return
        [
            ..methodSymbol.GetAttributes(),
            ..namedTypeSymbol.GetAttributes()
        ];
    }
}