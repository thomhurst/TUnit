using System.Collections.Generic;
using System.Collections.Immutable;
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
        return methodSymbol.GetAttribute(WellKnown.AttributeFullyQualifiedClasses.TimeoutAttribute, true);
            
    }
    
    public static AttributeData? GetDataDrivenTestAttribute(this IMethodSymbol methodSymbol)
    {
        return methodSymbol.GetAttribute(WellKnown.AttributeFullyQualifiedClasses.DataDrivenTest, false);
    }
    
    public static AttributeData? GetArgumentsAttribute(this IMethodSymbol methodSymbol)
    {
        return methodSymbol.GetAttribute(WellKnown.AttributeFullyQualifiedClasses.Arguments, false);
    }

    private static AttributeData? GetAttribute(this IMethodSymbol methodSymbol, string fullyQualifiedNameWithGlobalPrefix, bool searchClass = true)
    {
        IEnumerable<AttributeData> attributes = methodSymbol.GetAttributes();

        if (searchClass)
        {
            attributes = attributes.Concat(methodSymbol.ContainingType.GetAttributes());

        }
        return attributes.FirstOrDefault(x =>
                x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                == fullyQualifiedNameWithGlobalPrefix);
    }
}