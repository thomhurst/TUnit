using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers.Extensions;

/// <summary>
/// Shared by TUnit.Core.SourceGenerator and TUnit.Analyzers (linked into the analyzer project) so that the generator
/// and analyzer TUnit0001 infer a generic test method's type arguments from a typed data source the same way (#7116).
/// Reflection mode's <c>InferTypesFromTypedDataSource</c> applies the same rules to run-time types.
/// </summary>
internal static class TypedDataSourceInference
{
    /// <summary>
    /// Infers the type arguments of a generic test method on a non-generic class from a typed data source, whose
    /// type arguments come from its <c>DataSourceGeneratorAttribute</c> / <c>AsyncDataSourceGeneratorAttribute</c>
    /// base. One type parameter takes the first type argument; otherwise a single tuple argument with one element
    /// per type parameter, or one type argument per type parameter. A source deriving from
    /// <c>TypedDataSourceAttribute</c> without a generator base, such as <c>[Arguments&lt;T&gt;]</c>, instead
    /// matches its row type against the data parameters (<see cref="InferMethodTypeArgumentsFromRowType"/>).
    /// Returns <see langword="null"/> when nothing is inferred.
    /// </summary>
    public static ITypeSymbol[]? InferMethodTypeArguments(INamedTypeSymbol? attributeClass, IMethodSymbol method)
    {
        if (!method.IsGenericMethod || method.ContainingType.IsGenericType)
        {
            return null;
        }

        var hasGeneratorBase = false;
        for (var baseType = attributeClass?.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (IsTypedDataSourceBase(baseType))
            {
                hasGeneratorBase = true;
                if (InferMethodTypeArguments(method, baseType.TypeArguments) is { } inferred)
                {
                    return inferred;
                }
            }
            else if (!hasGeneratorBase && IsTypedRowBase(baseType))
            {
                return InferMethodTypeArgumentsFromRowType(method, baseType.TypeArguments[0]);
            }
        }

        return null;
    }

    /// <summary>
    /// The type arguments of generic test method <paramref name="method"/> inferred from the type arguments of a
    /// typed data source base: one type parameter takes the first type argument; otherwise a single tuple argument
    /// with one element per type parameter, or one type argument per type parameter. Returns
    /// <see langword="null"/> when nothing is inferred.
    /// </summary>
    public static ITypeSymbol[]? InferMethodTypeArguments(IMethodSymbol method, ImmutableArray<ITypeSymbol> typeArguments)
    {
        var typeParameterCount = method.TypeParameters.Length;
        if (typeParameterCount == 1 && typeArguments.Length >= 1)
        {
            return [typeArguments[0]];
        }

        if (typeArguments is [INamedTypeSymbol { IsTupleType: true } tupleType]
            && tupleType.TupleElements.Length == typeParameterCount)
        {
            return tupleType.TupleElements.Select(e => e.Type).ToArray();
        }

        if (typeArguments.Length == typeParameterCount)
        {
            return typeArguments.ToArray();
        }

        return null;
    }

    /// <summary>
    /// The type arguments of generic test method <paramref name="method"/> inferred from a typed row whose value
    /// has type <paramref name="rowType"/> (<c>[Arguments&lt;T&gt;]</c>, #7214). The row binds to the data
    /// parameters (any <c>CancellationToken</c> excluded): the whole row to a single data parameter, otherwise one
    /// tuple element per leading data parameter, or a scalar row to the first, with omitted trailing parameters
    /// optional. A <c>T?</c> parameter takes a value of its underlying type. Each parameter type is matched against its value type, including type
    /// arguments nested in generic parameter types, so <c>[Arguments&lt;int[]&gt;]</c> on
    /// <c>Run&lt;T&gt;(IEnumerable&lt;T&gt;)</c> infers nothing rather than <c>Run&lt;int[]&gt;</c>, whose
    /// parameter could not take the row. Every type parameter must be inferred, each to one type; otherwise
    /// returns <see langword="null"/>. Reflection mode's <c>InferTypesFromTypedRow</c> applies the same rules.
    /// </summary>
    public static ITypeSymbol[]? InferMethodTypeArgumentsFromRowType(IMethodSymbol method, ITypeSymbol rowType)
    {
        var dataParameters = method.Parameters.Where(p => !IsCancellationToken(p.Type)).ToArray();
        var requiredCount = dataParameters.Count(p => !p.IsOptional && !p.HasExplicitDefaultValue && !p.IsParams);
        var inferred = new ITypeSymbol?[method.TypeParameters.Length];

        if (dataParameters.Length == 1)
        {
            if (!MapTypeParameters(method, dataParameters[0].Type, rowType, inferred))
            {
                return null;
            }
        }
        else if (rowType is INamedTypeSymbol { IsTupleType: true } tupleType)
        {
            // The tuple spreads over the leading data parameters; omitted trailing ones take their defaults.
            var elements = tupleType.TupleElements;
            if (elements.Length < requiredCount || elements.Length > dataParameters.Length)
            {
                return null;
            }

            for (var i = 0; i < elements.Length; i++)
            {
                if (!MapTypeParameters(method, dataParameters[i].Type, elements[i].Type, inferred))
                {
                    return null;
                }
            }
        }
        else if (dataParameters.Length > 1 && requiredCount <= 1)
        {
            // A scalar row binds to the first data parameter; the rest are optional.
            if (!MapTypeParameters(method, dataParameters[0].Type, rowType, inferred))
            {
                return null;
            }
        }
        else
        {
            return null;
        }

        foreach (var typeArgument in inferred)
        {
            if (typeArgument is null)
            {
                return null;
            }
        }

        return inferred!;
    }

    /// <summary>
    /// Matches <paramref name="parameterType"/> against <paramref name="valueType"/>, recording each type
    /// parameter of <paramref name="method"/> it binds. Returns <see langword="false"/> when one type parameter
    /// binds two different types.
    /// </summary>
    private static bool MapTypeParameters(IMethodSymbol method, ITypeSymbol parameterType, ITypeSymbol valueType, ITypeSymbol?[] inferred)
    {
        if (parameterType is ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method } typeParameter)
        {
            if (!SymbolEqualityComparer.Default.Equals(typeParameter.DeclaringMethod?.OriginalDefinition, method.OriginalDefinition))
            {
                return true;
            }

            if (inferred[typeParameter.Ordinal] is { } existing && !SymbolEqualityComparer.Default.Equals(existing, valueType))
            {
                return false;
            }

            inferred[typeParameter.Ordinal] = valueType;
            return true;
        }

        // A T? parameter (Nullable<T>) takes a non-nullable value of its underlying type.
        if (parameterType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableParameter
            && valueType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
        {
            return MapTypeParameters(method, nullableParameter.TypeArguments[0], valueType, inferred);
        }

        if (parameterType is INamedTypeSymbol { IsGenericType: true } parameterNamed
            && valueType is INamedTypeSymbol { IsGenericType: true } valueNamed
            && SymbolEqualityComparer.Default.Equals(parameterNamed.OriginalDefinition, valueNamed.OriginalDefinition))
        {
            for (var i = 0; i < parameterNamed.TypeArguments.Length && i < valueNamed.TypeArguments.Length; i++)
            {
                if (!MapTypeParameters(method, parameterNamed.TypeArguments[i], valueNamed.TypeArguments[i], inferred))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool IsCancellationToken(ITypeSymbol type)
        => type is { Name: "CancellationToken", ContainingNamespace: { } containingNamespace }
            && containingNamespace.ToDisplayString() == "System.Threading";

    /// <summary>
    /// Whether <paramref name="baseType"/> is a constructed <c>DataSourceGeneratorAttribute</c> or
    /// <c>AsyncDataSourceGeneratorAttribute</c> from TUnit.Core.
    /// </summary>
    public static bool IsTypedDataSourceBase(INamedTypeSymbol baseType)
        => baseType.IsGenericType
            && baseType.OriginalDefinition.Name is "DataSourceGeneratorAttribute" or "AsyncDataSourceGeneratorAttribute"
            && baseType.OriginalDefinition.ContainingNamespace?.ToDisplayString() == "TUnit.Core";

    /// <summary>
    /// Whether <paramref name="baseType"/> is TUnit.Core's <c>TypedDataSourceAttribute&lt;T&gt;</c>, the base of
    /// <c>[Arguments&lt;T&gt;]</c>, whose row is a <c>T</c> value bound to the data parameters (#7214). The
    /// generator bases derive from it too, so a caller walking up from the attribute class treats it as a row
    /// base only when no generator base came first.
    /// </summary>
    public static bool IsTypedRowBase(INamedTypeSymbol baseType)
        => baseType is { IsGenericType: true, TypeArguments.Length: 1 }
            && baseType.OriginalDefinition.Name == "TypedDataSourceAttribute"
            && baseType.OriginalDefinition.ContainingNamespace?.ToDisplayString() == "TUnit.Core";
}
