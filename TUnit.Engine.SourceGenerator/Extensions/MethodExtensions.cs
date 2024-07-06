using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.Extensions;

public static class MethodExtensions
{
    public static AttributeData? GetTestAttribute(this IMethodSymbol methodSymbol)
    {
        return methodSymbol.GetAttributes()
            .SafeFirstOrDefault(x => x.AttributeClass?.BaseType?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) 
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

    public static IEnumerable<IParameterSymbol> ParametersWithoutTimeoutCancellationToken(
        this IMethodSymbol methodSymbol)
    {
        if (methodSymbol.Parameters.IsDefaultOrEmpty)
        {
            return [];
        }

        if (methodSymbol.Parameters.Last().Type
                .ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) ==
            ClassNames.CancellationToken)
        {
            return methodSymbol.Parameters.Take(methodSymbol.Parameters.Length - 1);
        }

        return methodSymbol.Parameters;
    }
}