using System.Collections.Generic;
using System.Collections.Immutable;
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
        if (!existingGenericParams.Contains(PreferredName))
        {
            return PreferredName;
        }

        var candidate = PreferredName + "_";
        while (existingGenericParams.Contains(candidate))
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
    /// Generates the context mapping expression for covariant assertions.
    /// Uses nullable cast since Map's Func takes TValue? and returns TNew?.
    /// </summary>
    public static string GetCovariantContextExpr(string targetTypeName)
    {
        var nullableCastType = targetTypeName.EndsWith("?") ? targetTypeName : $"{targetTypeName}?";
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

    /// <summary>
    /// Determines whether EVERY one of an assertion class's own type parameters can be inferred by
    /// the C# compiler from the constructor's value parameters (the parameters after the leading
    /// <c>AssertionContext&lt;T&gt;</c>). When they all can, the caller never names a type argument,
    /// the covariant <c>&lt;TActual, T...&gt;</c> overload binds on its own, and the inference-friendly
    /// pinned-receiver overload would be pure dead weight — so callers of this gate the pinned overload
    /// on the negation. See issue #5922.
    /// </summary>
    public static bool OwnGenericsAreInferable(IMethodSymbol constructor, ImmutableArray<ITypeParameterSymbol> ownTypeParameters)
    {
        if (ownTypeParameters.IsDefaultOrEmpty)
        {
            return true;
        }

        var valueParameters = constructor.Parameters.Skip(1).ToArray();

        foreach (var typeParameter in ownTypeParameters)
        {
            var inferable = false;
            foreach (var parameter in valueParameters)
            {
                if (AppearsInInferablePosition(parameter.Type, typeParameter))
                {
                    inferable = true;
                    break;
                }
            }

            if (!inferable)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Recursively determines whether <paramref name="typeParameter"/> appears in a position the
    /// compiler can infer from an argument's type. Recurses through named-type arguments and array
    /// elements, but stops at delegate boundaries (a lambda argument does not fix a delegate's type
    /// parameters) and at <c>System.Linq.Expressions.Expression&lt;...&gt;</c>. The bias is
    /// conservative: anything we cannot prove inferable is treated as non-inferable, which keeps the
    /// pinned overload (the pre-existing behaviour) rather than risk an ergonomic regression.
    /// </summary>
    private static bool AppearsInInferablePosition(ITypeSymbol type, ITypeParameterSymbol typeParameter)
    {
        if (SymbolEqualityComparer.Default.Equals(type, typeParameter))
        {
            return true;
        }

        // A lambda/method-group argument does not fix a delegate's type parameters, and the same is
        // true for an expression-tree built from one — so type parameters reachable only through these
        // are not inferable.
        if (type.TypeKind == TypeKind.Delegate || IsExpressionTree(type))
        {
            return false;
        }

        if (type is IArrayTypeSymbol arrayType)
        {
            return AppearsInInferablePosition(arrayType.ElementType, typeParameter);
        }

        if (type is INamedTypeSymbol namedType)
        {
            foreach (var typeArgument in namedType.TypeArguments)
            {
                if (AppearsInInferablePosition(typeArgument, typeParameter))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsExpressionTree(ITypeSymbol type)
    {
        return type is INamedTypeSymbol { Name: "Expression", IsGenericType: true } named
            && named.ContainingNamespace?.ToDisplayString() == "System.Linq.Expressions";
    }
}
