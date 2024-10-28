using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers.Extensions;

public static class MethodExtensions
{
    public static bool IsTestMethod(this IMethodSymbol methodSymbol, Compilation compilation)
    {
        var testAttribute = compilation.GetTypeByMetadataName("TUnit.Core.TestAttribute")!;
        return methodSymbol.GetAttributes().Any(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, testAttribute));
    }

    public static bool IsHookMethod(this IMethodSymbol methodSymbol, Compilation compilation)
    {
        return IsNonGlobalHookMethod(methodSymbol, compilation) || IsGlobalHookMethod(methodSymbol, compilation);
    }
    
    public static bool IsNonGlobalHookMethod(this IMethodSymbol methodSymbol, Compilation compilation)
    {
        return methodSymbol.GetAttributes().Any(x => x.IsNonGlobalHook(compilation));
    }
    
    public static bool IsGlobalHookMethod(this IMethodSymbol methodSymbol, Compilation compilation)
    {
        return methodSymbol.GetAttributes().Any(x => x.IsGlobalHook(compilation));
    }
    
    public static bool HasTimeoutAttribute(this IMethodSymbol methodSymbol, out AttributeData? timeoutAttribute)
    {
        timeoutAttribute = GetTimeoutAttribute(methodSymbol);
        
        return timeoutAttribute != null;
    }
    
    public static AttributeData? GetTimeoutAttribute(this IMethodSymbol methodSymbol)
    {
        return methodSymbol.GetAttribute(WellKnown.AttributeFullyQualifiedClasses.TimeoutAttribute.WithGlobalPrefix, true);
    }
    
    public static AttributeData? GetArgumentsAttribute(this IMethodSymbol methodSymbol)
    {
        return methodSymbol.GetAttribute(WellKnown.AttributeFullyQualifiedClasses.Arguments.WithGlobalPrefix, false);
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