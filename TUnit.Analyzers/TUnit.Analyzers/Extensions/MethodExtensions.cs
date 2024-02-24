using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers.Extensions;

public static class MethodExtensions
{
    public static bool HasTimeoutAttribute(this IMethodSymbol methodSymbol, out AttributeData? timeoutAttribute)
    {
        timeoutAttribute = GetTimeoutAttribute(methodSymbol);
        
        return timeoutAttribute != null;
    }
    
    public static AttributeData? GetTimeoutAttribute(this IMethodSymbol methodSymbol)
    {
        return methodSymbol.GetAttributes()
            .Concat(methodSymbol.ContainingType.GetAttributes())
            .FirstOrDefault(x =>
                x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                == WellKnown.AttributeFullyQualifiedClasses.TimeoutAttribute);
    }
}