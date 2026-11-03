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
    /// parameters (any <c>CancellationToken</c> excluded): a tuple row spreads one element per leading data parameter,
    /// as <c>ToObjectArray</c> spreads it, an <c>object[]</c>, <c>System.Tuple</c> or other <c>ITuple</c> row infers nothing, and any other
    /// row binds whole to a single data parameter, or to the first of several, with omitted trailing parameters
    /// optional (params arrays and params collections included). A nullable tuple row infers nothing: a non-null
    /// value boxes to its tuple and spreads, but a null one binds whole, so no instantiation fits both. A <c>Func&lt;T&gt;</c>, <c>TestDataRow&lt;T&gt;</c> or
    /// <c>TestDataRow&lt;Func&lt;T&gt;&gt;</c> row binds as its <c>T</c>, since it is invoked and unwrapped before invocation, and
    /// a reference-type array <c>T</c> there spreads, so its element binds to the data parameter. A <c>T?</c> parameter takes a value of its underlying
    /// type. Each parameter type is matched against its value type, including type
    /// arguments nested in generic parameter types, including implemented interfaces and base classes. All matching constructed types must agree
    /// on the bindings, so ambiguous interfaces infer nothing regardless of enumeration order. Every type parameter must be inferred, each to one type; otherwise
    /// returns <see langword="null"/>. Reflection mode's <c>InferTypesFromTypedRow</c> applies the same rules.
    /// </summary>
    public static ITypeSymbol[]? InferMethodTypeArgumentsFromRowType(IMethodSymbol method, ITypeSymbol rowType)
    {
        var rowTypes = GetRowArgumentTypes(rowType);
        if (rowTypes is null)
        {
            return null;
        }

        var dataParameters = method.Parameters.Where(p => !IsCancellationToken(p.Type)).ToArray();
        var requiredCount = dataParameters.Count(p => !p.IsOptional && !p.HasExplicitDefaultValue && !p.IsParams);
        var inferred = new ITypeSymbol?[method.TypeParameters.Length];

        if (rowTypes.Value.Length < requiredCount || rowTypes.Value.Length > dataParameters.Length)
        {
            return null;
        }

        for (var i = 0; i < rowTypes.Value.Length; i++)
        {
            if (!MapTypeParameters(method, dataParameters[i].Type, rowTypes.Value[i], inferred))
            {
                return null;
            }
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

    // The argument types after the same invocation, unwrapping and spreading used by discovery.
    // Null means the row shape does not declare its element types reliably.
    public static ImmutableArray<ITypeSymbol>? GetRowArgumentTypes(ITypeSymbol rowType)
    {
        // The row reaches the method as ToObjectArray and TestDataRowUnwrapper.UnwrapArray produce it: a
        // Func<T> row is invoked once, a TestDataRow<T> row is unwrapped, and a Func<T> inside it is invoked
        // once, so the value binds as that T.
        rowType = UnwrapSingleArgument(rowType, "Func", "System");
        if (IsNullableTuple(rowType))
        {
            return null;
        }

        var testDataRowType = FindTestDataRowType(rowType);
        var spreadsReferenceArray = false;
        var isTestDataRow = testDataRowType is not null;
        if (testDataRowType is not null)
        {
            rowType = UnwrapSingleArgument(testDataRowType.TypeArguments[0], "Func", "System");
            if (IsNullableTuple(rowType))
            {
                return null;
            }

            // UnwrapArray spreads a TestDataRow value that is an object?[], which a reference-type array
            // is through array covariance (ToObjectArray keeps a bare one whole). The elements bind as
            // their declared type; an object or dynamic element type declares nothing, so leave it to
            // runtime inference.
            if (rowType is IArrayTypeSymbol { Rank: 1, ElementType.IsReferenceType: true } spreadArray)
            {
                if (spreadArray.ElementType.SpecialType == SpecialType.System_Object
                    || spreadArray.ElementType.TypeKind == TypeKind.Dynamic)
                {
                    return null;
                }

                rowType = spreadArray.ElementType;
                spreadsReferenceArray = true;
            }
        }

        if (!spreadsReferenceArray && rowType is INamedTypeSymbol { IsTupleType: true } tupleType)
        {
            var elements = tupleType.TupleElements;
            if (!isTestDataRow && elements.Length == 1 && IsTestDataRow(elements[0].Type))
            {
                return GetRowArgumentTypes(elements[0].Type);
            }

            return elements.Select(e => e.Type).ToImmutableArray();
        }

        if (!spreadsReferenceArray && IsSpreadWithoutElementTypes(rowType))
        {
            return null;
        }

        return ImmutableArray.Create(rowType);
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

        if (parameterType is IArrayTypeSymbol parameterArray
            && valueType is IArrayTypeSymbol valueArray
            && parameterArray.Rank == valueArray.Rank
            && parameterArray.IsSZArray == valueArray.IsSZArray)
        {
            return MapTypeParameters(method, parameterArray.ElementType, valueArray.ElementType, inferred);
        }

        // A T? parameter (Nullable<T>) takes a non-nullable value of its underlying type.
        if (parameterType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableParameter
            && valueType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
        {
            return MapTypeParameters(method, nullableParameter.TypeArguments[0], valueType, inferred);
        }

        if (parameterType is INamedTypeSymbol { IsGenericType: true } parameterNamed)
        {
            // Inspect every matching construction: choosing the first interface would make ambiguous
            // rows depend on metadata order. Repeated identical bindings (including diamonds) are safe.
            for (var current = valueType as INamedTypeSymbol; current is not null; current = current.BaseType)
            {
                if (!MapMatchingType(current))
                {
                    return false;
                }
            }

            foreach (var implemented in valueType.AllInterfaces)
            {
                if (!MapMatchingType(implemented))
                {
                    return false;
                }
            }

            bool MapMatchingType(INamedTypeSymbol candidate)
            {
                if (!SymbolEqualityComparer.Default.Equals(parameterNamed.OriginalDefinition, candidate.OriginalDefinition))
                {
                    return true;
                }

                // Roslyn keeps outer generic arguments on ContainingType; reflection's
                // GetGenericArguments includes them in the nested type's arguments.
                if (parameterNamed.ContainingType is { } containingParameter
                    && candidate.ContainingType is { } containingCandidate
                    && !MapTypeParameters(method, containingParameter, containingCandidate, inferred))
                {
                    return false;
                }

                for (var i = 0; i < parameterNamed.TypeArguments.Length; i++)
                {
                    if (!MapTypeParameters(method, parameterNamed.TypeArguments[i], candidate.TypeArguments[i], inferred))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        return true;
    }

    /// <summary>
    /// Whether <paramref name="type"/> is a <c>ValueTuple&lt;...&gt;?</c>. A non-null value boxes to its underlying
    /// tuple, which <c>ToObjectArray</c> and <c>UnwrapArray</c> then spread, but a null value binds whole as null,
    /// so the row's shape depends on its value and is left to runtime inference.
    /// </summary>
    private static bool IsNullableTuple(ITypeSymbol type)
        => type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments: [INamedTypeSymbol { IsTupleType: true }] };

    private static ITypeSymbol UnwrapSingleArgument(ITypeSymbol type, string name, string containingNamespace)
        => type is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } namedType
            && namedType.OriginalDefinition.Name == name
            && namedType.OriginalDefinition.ContainingNamespace?.ToDisplayString() == containingNamespace
                ? namedType.TypeArguments[0]
                : type;

    private static bool IsTestDataRow(ITypeSymbol type)
        => FindTestDataRowType(type) is not null;

    /// <summary>
    /// The constructed <c>TestDataRow&lt;T&gt;</c> that <paramref name="type"/> is or derives from, or
    /// <see langword="null"/>. <c>TestDataRowUnwrapper</c> unwraps any row through <c>ITestDataRow</c>, so a
    /// record deriving from <c>TestDataRow&lt;T&gt;</c> binds as its <c>T</c> as well.
    /// </summary>
    private static INamedTypeSymbol? FindTestDataRowType(ITypeSymbol type)
    {
        for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            if (current is { IsGenericType: true, TypeArguments.Length: 1 }
                && current.OriginalDefinition.Name == "TestDataRow"
                && current.OriginalDefinition.ContainingNamespace?.ToDisplayString() == "TUnit.Core")
            {
                return current;
            }
        }

        return null;
    }

    private static bool IsSpreadWithoutElementTypes(ITypeSymbol type)
        => type is IArrayTypeSymbol { Rank: 1, ElementType.SpecialType: SpecialType.System_Object }
            || type is INamedTypeSymbol { IsGenericType: true, Name: "Tuple", ContainingNamespace: { } tupleNamespace }
                && tupleNamespace.ToDisplayString() == "System"
            || IsITuple(type)
            || type.AllInterfaces.Any(IsITuple);

    // DataSourceHelpers.IsTuple spreads any runtime ITuple, so an ITuple row type, or one implementing it (other
    // than the ValueTuple rows matched above), declares no element types.
    private static bool IsITuple(ITypeSymbol type)
        => type is { Name: "ITuple", ContainingNamespace: { } containingNamespace }
            && containingNamespace.ToDisplayString() == "System.Runtime.CompilerServices";

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
