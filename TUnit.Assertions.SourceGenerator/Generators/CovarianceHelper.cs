using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Assertions.SourceGenerator.Generators;

/// <summary>
/// Shared helpers for covariant assertion generation across MethodAssertionGenerator and AssertionExtensionGenerator.
/// </summary>
internal static class CovarianceHelper
{
    private const string PreferredName = "TActual";

    /// <summary>
    /// Returns a type parameter name for the covariant source type that doesn't conflict
    /// with any existing generic parameters on the method.
    /// </summary>
    public static string GetCovariantTypeParamName(IEnumerable<string> existingGenericParams)
    {
        var existing = new HashSet<string>(existingGenericParams);
        if (!existing.Contains(PreferredName))
        {
            return PreferredName;
        }

        // Append underscores until unique
        var candidate = PreferredName + "_";
        while (existing.Contains(candidate))
        {
            candidate += "_";
        }
        return candidate;
    }

    /// <summary>
    /// Determines if a target type supports covariant assertions.
    /// Returns true for interfaces and non-sealed classes that don't contain unresolved type parameters.
    /// </summary>
    public static bool IsCovariantCandidate(ITypeSymbol type)
    {
        return (type.TypeKind == TypeKind.Interface || type.TypeKind == TypeKind.Class)
            && !type.IsSealed
            && !ContainsTypeParameter(type);
    }

    /// <summary>
    /// Returns the type name suitable for use in a generic constraint (strips trailing nullable annotation).
    /// </summary>
    public static string GetConstraintTypeName(string typeName, ITypeSymbol type)
    {
        if (type.NullableAnnotation == NullableAnnotation.Annotated && typeName.EndsWith("?"))
        {
            return typeName.Substring(0, typeName.Length - 1);
        }
        return typeName;
    }

    /// <summary>
    /// Returns the nullable cast form of a type name for use in Map lambdas.
    /// Map's Func takes TValue? and returns TNew?, so the cast must be to the nullable form.
    /// </summary>
    public static string GetNullableCastType(string typeName)
    {
        return typeName.EndsWith("?") ? typeName : $"{typeName}?";
    }

    /// <summary>
    /// Generates the context mapping expression for covariant assertions.
    /// </summary>
    public static string GetCovariantContextExpr(string targetTypeName)
    {
        var nullableCastType = GetNullableCastType(targetTypeName);
        return $"source.Context.Map<{targetTypeName}>(static x => ({nullableCastType})x)";
    }

    /// <summary>
    /// Recursively checks if a type symbol contains any unresolved type parameters.
    /// Types like Lazy&lt;T&gt; would break type inference if made covariant.
    /// </summary>
    public static bool ContainsTypeParameter(ITypeSymbol type)
    {
        if (type is ITypeParameterSymbol)
        {
            return true;
        }

        if (type is INamedTypeSymbol namedType)
        {
            foreach (var typeArg in namedType.TypeArguments)
            {
                if (ContainsTypeParameter(typeArg))
                {
                    return true;
                }
            }
        }

        if (type is IArrayTypeSymbol arrayType)
        {
            return ContainsTypeParameter(arrayType.ElementType);
        }

        return false;
    }
}
