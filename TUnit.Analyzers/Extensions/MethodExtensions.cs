using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers.Extensions;

public static class MethodExtensions
{
    /// <summary>
    /// Returns true if the method is decorated with the exact <c>TUnit.Core.TestAttribute</c>.
    /// </summary>
    /// <remarks>
    /// This is an exact-match check — it does not match subclasses of <c>BaseTestAttribute</c>
    /// (e.g. <c>[DynamicTestBuilder]</c>). Use <see cref="HasTestAttribute"/> for the broader,
    /// inheritance-aware check.
    /// </remarks>
    public static bool IsTestMethod(this IMethodSymbol methodSymbol, Compilation compilation)
    {
        var testAttribute = compilation.GetTypeByMetadataName("TUnit.Core.TestAttribute")!;
        return methodSymbol.GetAttributes().Any(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, testAttribute));
    }

    /// <summary>
    /// Returns true if the method is decorated with any attribute deriving from
    /// <c>TUnit.Core.BaseTestAttribute</c> (e.g. <c>[Test]</c> or <c>[DynamicTestBuilder]</c>).
    /// </summary>
    public static bool HasTestAttribute(this IMethodSymbol methodSymbol, Compilation compilation)
    {
        var baseTestAttribute = compilation.GetTypeByMetadataName("TUnit.Core.BaseTestAttribute");

        if (baseTestAttribute is null)
        {
            return false;
        }

        // Use Any(...) rather than Contains(...): the latter walks the whole base-type chain even
        // after a match, whereas Any short-circuits on the first matching type.
        return methodSymbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.GetSelfAndBaseTypes()
                .Any(t => SymbolEqualityComparer.Default.Equals(t, baseTestAttribute)) == true);
    }

    public static bool IsHookMethod(this IMethodSymbol methodSymbol, Compilation compilation, [NotNullWhen(true)] out INamedTypeSymbol? type, [NotNullWhen(true)] out HookLevel? hookLevel, [NotNullWhen(true)] out HookType? hookType)
    {
        return IsStandardHookMethod(methodSymbol, compilation, out type, out hookLevel, out hookType) || IsEveryHookMethod(methodSymbol, compilation, out type, out hookLevel, out hookType);
    }

    public static bool IsStandardHookMethod(this IMethodSymbol methodSymbol, Compilation compilation, [NotNullWhen(true)] out INamedTypeSymbol? type, [NotNullWhen(true)] out HookLevel? hookLevel, [NotNullWhen(true)] out HookType? hookType)
    {
        foreach (var attributeData in methodSymbol.GetAttributes())
        {
            if (attributeData.IsStandardHook(compilation, out type, out hookLevel, out hookType))
            {
                return true;
            }
        }

        hookType = null;
        type = null;
        hookLevel = null;
        return false;
    }

    public static bool IsEveryHookMethod(this IMethodSymbol methodSymbol, Compilation compilation, [NotNullWhen(true)] out INamedTypeSymbol? type, [NotNullWhen(true)] out HookLevel? hookLevel, [NotNullWhen(true)] out HookType? hookType)
    {
        foreach (var attributeData in methodSymbol.GetAttributes())
        {
            if (attributeData.IsEveryHook(compilation, out type, out hookLevel, out hookType))
            {
                return true;
            }
        }

        hookType = null;
        type = null;
        hookLevel = null;
        return false;
    }

    public static AttributeData? GetTimeoutAttribute(this IMethodSymbol methodSymbol)
    {
        return methodSymbol.GetAttribute(WellKnown.AttributeFullyQualifiedClasses.TimeoutAttribute.WithGlobalPrefix);
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
                x.AttributeClass?.GloballyQualifiedNonGeneric()
                == fullyQualifiedNameWithGlobalPrefix);
    }
}
