﻿using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers.Extensions;

public static class MethodExtensions
{
    public static bool IsTestMethod(this IMethodSymbol methodSymbol, Compilation compilation)
    {
        var testAttribute = compilation.GetTypeByMetadataName("TUnit.Core.TestAttribute")!;
        return methodSymbol.GetAttributes().Any(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, testAttribute));
    }

    public static bool IsHookMethod(this IMethodSymbol methodSymbol, Compilation compilation, [NotNullWhen(true)] out INamedTypeSymbol? type, [NotNullWhen(true)] out HookLevel? hookLevel)
    {
        return IsStandardHookMethod(methodSymbol, compilation, out type, out hookLevel) || IsEveryHookMethod(methodSymbol, compilation, out type, out hookLevel);
    }
    
    public static bool IsStandardHookMethod(this IMethodSymbol methodSymbol, Compilation compilation, [NotNullWhen(true)] out INamedTypeSymbol? type, [NotNullWhen(true)] out HookLevel? hookLevel)
    {
        foreach (var attributeData in methodSymbol.GetAttributes())
        {
            if (attributeData.IsStandardHook(compilation, out type, out hookLevel))
            {
                return true;
            }
        }

        type = null;
        hookLevel = null;
        return false;
    }
    
    public static bool IsEveryHookMethod(this IMethodSymbol methodSymbol, Compilation compilation, [NotNullWhen(true)] out INamedTypeSymbol? type, [NotNullWhen(true)] out HookLevel? hookLevel)
    {
        foreach (var attributeData in methodSymbol.GetAttributes())
        {
            if (attributeData.IsEveryHook(compilation, out type, out hookLevel))
            {
                return true;
            }
        }

        type = null;
        hookLevel = null;
        return false;
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