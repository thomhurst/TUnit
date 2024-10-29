using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Extensions;

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
    
    public static bool IsHook(this IMethodSymbol methodSymbol, Compilation compilation)
    {
        return methodSymbol.GetAttributes().Any(x => x.IsNonGlobalHook(compilation) || x.IsGlobalHook(compilation));
    }

    public static AttributeData[] GetAttributesIncludingClass(this IMethodSymbol methodSymbol, INamedTypeSymbol namedTypeSymbol)
    {
        return GetAttributesIncludingClassEnumerable(methodSymbol, namedTypeSymbol).ToArray();
    }
    
    public static IEnumerable<AttributeData> GetAttributesIncludingClassEnumerable(this IMethodSymbol methodSymbol, INamedTypeSymbol namedTypeSymbol)
    {
        foreach (var attributeData in methodSymbol.GetAttributes())
        {
            yield return attributeData;
        }

        var type = namedTypeSymbol;

        while (type != null)
        {
            foreach (var attributeData in type.GetAttributes())
            {
                yield return attributeData;
            }
            
            type = type.BaseType;
        }
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
            WellKnownFullyQualifiedClassNames.CancellationToken.WithGlobalPrefix)
        {
            return methodSymbol.Parameters.Take(methodSymbol.Parameters.Length - 1);
        }

        return methodSymbol.Parameters;
    }
}