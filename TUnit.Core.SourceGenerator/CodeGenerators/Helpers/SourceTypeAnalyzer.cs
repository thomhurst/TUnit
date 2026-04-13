using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

/// <summary>
/// Holds per-position source type information for data source attributes.
/// Each position may have zero, one, or multiple possible source types.
/// A null entry at a position means the type is unknown (falls back to CastHelper.Cast).
/// </summary>
internal sealed class SourceTypeInfo
{
    private readonly IReadOnlyList<ITypeSymbol>?[] _typesPerPosition;

    public SourceTypeInfo(IReadOnlyList<ITypeSymbol>?[] typesPerPosition)
    {
        _typesPerPosition = typesPerPosition;
    }

    public int Length => _typesPerPosition.Length;

    /// <summary>
    /// Returns all known source types at the given position, or null if unknown/out-of-range.
    /// </summary>
    public IReadOnlyList<ITypeSymbol>? GetTypes(int index)
    {
        return (uint)index < (uint)_typesPerPosition.Length ? _typesPerPosition[index] : null;
    }
}

internal static class SourceTypeAnalyzer
{
    /// <summary>
    /// Analyzes data source attributes on a method to determine the compile-time source type
    /// for each parameter position. Returns null if no data sources exist.
    /// A null entry at a position means that position's source type is unknown (falls back to CastHelper.Cast).
    /// </summary>
    public static SourceTypeInfo? GetMethodParameterSourceTypes(IMethodSymbol method)
    {
        var dataSources = new List<AttributeData>();

        foreach (var attr in method.GetAttributes())
        {
            if (!DataSourceAttributeHelper.IsDataSourceAttribute(attr.AttributeClass))
            {
                continue;
            }

            dataSources.Add(attr);
        }

        // No data sources at all -> no source type info
        if (dataSources.Count == 0)
        {
            return null;
        }

        // Filter to non-CancellationToken parameters
        var parameterCount = 0;
        var hasParams = false;
        foreach (var p in method.Parameters)
        {
            if (p.Type.Name != "CancellationToken" || p.Type.ContainingNamespace?.ToString() != "System.Threading")
            {
                parameterCount++;
                if (p.IsParams)
                {
                    hasParams = true;
                }
            }
        }

        if (parameterCount == 0)
        {
            return null;
        }

        // For params methods with [Arguments], size the array to max argument count
        var arraySize = hasParams ? GetMaxArgumentCount(dataSources, parameterCount) : parameterCount;

        return ExtractSourceTypesFromAllAttributes(dataSources, arraySize);
    }

    /// <summary>
    /// Analyzes class-level data source attributes to determine source types for constructor parameters.
    /// </summary>
    public static SourceTypeInfo? GetConstructorParameterSourceTypes(INamedTypeSymbol? classType)
    {
        if (classType == null)
        {
            return null;
        }

        var dataSources = new List<AttributeData>();

        foreach (var attr in classType.GetAttributes())
        {
            if (!DataSourceAttributeHelper.IsDataSourceAttribute(attr.AttributeClass))
            {
                continue;
            }

            dataSources.Add(attr);
        }

        if (dataSources.Count == 0)
        {
            return null;
        }

        // Use the same constructor selection as InstanceFactoryGenerator to ensure
        // source type analysis targets the constructor that will actually be called.
        var constructor = InstanceFactoryGenerator.GetPrimaryConstructor(classType);

        if (constructor == null || constructor.Parameters.Length == 0)
        {
            return null;
        }

        var ctorParamCount = constructor.Parameters.Length;
        var ctorHasParams = constructor.Parameters[ctorParamCount - 1].IsParams;
        var ctorArraySize = ctorHasParams ? GetMaxArgumentCount(dataSources, ctorParamCount) : ctorParamCount;

        return ExtractSourceTypesFromAllAttributes(dataSources, ctorArraySize);
    }

    /// <summary>
    /// Processes ALL data source attributes and merges per-position source types.
    /// </summary>
    private static SourceTypeInfo ExtractSourceTypesFromAllAttributes(
        List<AttributeData> dataSources,
        int parameterCount)
    {
        // Each position accumulates a set of possible source types.
        // null = unknown (some attribute couldn't determine the type for this position).
        var positionTypes = new List<ITypeSymbol>?[parameterCount];
        var positionUnknown = new bool[parameterCount];

        foreach (var attr in dataSources)
        {
            if (IsArgumentsAttribute(attr))
            {
                ProcessArgumentsAttribute(attr, positionTypes, positionUnknown, parameterCount);
            }
            else if (attr.AttributeClass != null && DataSourceAttributeHelper.IsTypedDataSourceAttribute(attr.AttributeClass))
            {
                ProcessTypedDataSource(attr, positionTypes, positionUnknown, parameterCount);
            }
            else
            {
                // Unknown data source type (e.g., MethodDataSource, non-generic ClassDataSource
                // without typeof args) -> mark ALL positions as unknown
                for (var i = 0; i < parameterCount; i++)
                {
                    positionUnknown[i] = true;
                }
            }
        }

        // Build the final result: null entries where unknown
        var result = new IReadOnlyList<ITypeSymbol>?[parameterCount];
        for (var i = 0; i < parameterCount; i++)
        {
            if (positionUnknown[i] || positionTypes[i] == null || positionTypes[i]!.Count == 0)
            {
                result[i] = null;
            }
            else
            {
                result[i] = positionTypes[i];
            }
        }

        return new SourceTypeInfo(result);
    }

    private static void ProcessArgumentsAttribute(
        AttributeData attr,
        List<ITypeSymbol>?[] positionTypes,
        bool[] positionUnknown,
        int parameterCount)
    {
        var values = GetArgumentValues(attr);

        if (values == null)
        {
            // Can't extract values from this attribute -> mark all positions as unknown
            for (var i = 0; i < parameterCount; i++)
            {
                positionUnknown[i] = true;
            }

            return;
        }

        for (var i = 0; i < parameterCount && i < values.Value.Length; i++)
        {
            if (positionUnknown[i])
            {
                continue;
            }

            var tc = values.Value[i];

            // Null literal or error -> unknown source type for this position
            if (tc.IsNull || tc.Kind == TypedConstantKind.Error || tc.Type == null)
            {
                positionUnknown[i] = true;
                continue;
            }

            AddTypeToPosition(positionTypes, i, tc.Type);
        }
    }

    /// <summary>
    /// Processes any attribute implementing ITypedDataSourceAttribute&lt;T&gt;.
    /// For single-generic attributes (e.g., ClassDataSourceAttribute&lt;T&gt;), T maps to position 0.
    /// For multi-generic attributes, each type argument maps to its corresponding position.
    /// Works with ClassDataSourceAttribute&lt;T&gt; and any user-created custom data source
    /// that extends TypedDataSourceAttribute&lt;T&gt;.
    /// </summary>
    private static void ProcessTypedDataSource(
        AttributeData attr,
        List<ITypeSymbol>?[] positionTypes,
        bool[] positionUnknown,
        int parameterCount)
    {
        var attrClass = attr.AttributeClass;

        if (attrClass == null)
        {
            return;
        }

        if (attrClass.IsGenericType)
        {
            var typeArgs = attrClass.TypeArguments;

            if (typeArgs.Length == 1)
            {
                // Single generic: e.g., ClassDataSourceAttribute<T> -> T maps to position 0
                var sourceType = DataSourceAttributeHelper.GetTypedDataSourceType(attrClass);

                if (sourceType != null && parameterCount > 0 && !positionUnknown[0])
                {
                    AddTypeToPosition(positionTypes, 0, sourceType);
                }
            }
            else
            {
                // Multi-generic: e.g., ClassDataSourceAttribute<T1, T2, ...> -> each Tn maps to position n
                for (var i = 0; i < parameterCount && i < typeArgs.Length; i++)
                {
                    if (positionUnknown[i])
                    {
                        continue;
                    }

                    var typeArg = typeArgs[i];

                    if (typeArg is IErrorTypeSymbol)
                    {
                        positionUnknown[i] = true;
                    }
                    else
                    {
                        AddTypeToPosition(positionTypes, i, typeArg);
                    }
                }
            }
        }
        else
        {
            // Non-generic typed data source: try to extract T from ITypedDataSourceAttribute<T>
            var sourceType = DataSourceAttributeHelper.GetTypedDataSourceType(attrClass);

            if (sourceType != null && parameterCount > 0 && !positionUnknown[0])
            {
                AddTypeToPosition(positionTypes, 0, sourceType);
            }
            else
            {
                // Can't determine source type statically -> mark all positions as unknown
                for (var i = 0; i < parameterCount; i++)
                {
                    positionUnknown[i] = true;
                }
            }
        }
    }

    /// <summary>
    /// Adds a source type to the given position, deduplicating with SymbolEqualityComparer.
    /// </summary>
    private static void AddTypeToPosition(List<ITypeSymbol>?[] positionTypes, int index, ITypeSymbol type)
    {
        if (positionTypes[index] == null)
        {
            positionTypes[index] = new List<ITypeSymbol> { type };
            return;
        }

        // Deduplicate
        foreach (var existing in positionTypes[index]!)
        {
            if (SymbolEqualityComparer.Default.Equals(existing, type))
            {
                return;
            }
        }

        positionTypes[index]!.Add(type);
    }

    private static int GetMaxArgumentCount(List<AttributeData> dataSources, int baseCount)
    {
        foreach (var attr in dataSources)
        {
            if (!IsArgumentsAttribute(attr))
            {
                continue;
            }

            var values = GetArgumentValues(attr);
            if (values != null && values.Value.Length > baseCount)
            {
                baseCount = values.Value.Length;
            }
        }

        return baseCount;
    }

    /// <summary>
    /// Extracts the TypedConstant values from an [Arguments] attribute.
    /// For non-generic: ConstructorArguments[0] is a params array.
    /// For generic (Arguments&lt;T&gt;): ConstructorArguments[0] is the single typed value.
    /// </summary>
    private static ImmutableArray<TypedConstant>? GetArgumentValues(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length == 0)
        {
            return null;
        }

        var firstArg = attr.ConstructorArguments[0];

        // Non-generic [Arguments(params object?[] args)] — the first arg is an array
        if (firstArg.Kind == TypedConstantKind.Array)
        {
            return firstArg.Values.IsDefault ? null : firstArg.Values;
        }

        // Generic [Arguments<T>(T value)] — single typed value
        // Return all constructor args as individual values
        return attr.ConstructorArguments;
    }

    private static bool IsArgumentsAttribute(AttributeData attr)
    {
        return attr.AttributeClass?.Name is "ArgumentsAttribute"
            && attr.AttributeClass.ToDisplayString() == WellKnownFullyQualifiedClassNames.ArgumentsAttribute.WithoutGlobalPrefix;
    }
}
