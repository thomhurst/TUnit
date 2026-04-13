using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

internal static class SourceTypeAnalyzer
{
    /// <summary>
    /// Analyzes data source attributes on a method to determine the compile-time source type
    /// for each parameter position. Returns null if source types cannot be determined
    /// (e.g., dynamic data sources exist). A null element in the returned array means
    /// that position's source type is unknown (falls back to CastHelper.Cast).
    /// </summary>
    public static ITypeSymbol?[]? GetMethodParameterSourceTypes(IMethodSymbol method)
    {
        var dataSources = new List<AttributeData>();
        var argumentsAttributes = new List<AttributeData>();

        foreach (var attr in method.GetAttributes())
        {
            if (!DataSourceAttributeHelper.IsDataSourceAttribute(attr.AttributeClass))
            {
                continue;
            }

            dataSources.Add(attr);

            if (IsArgumentsAttribute(attr))
            {
                argumentsAttributes.Add(attr);
            }
        }

        // No data sources at all → no source type info
        if (dataSources.Count == 0)
        {
            return null;
        }

        // If ANY non-Arguments data source exists, we can't guarantee types
        if (argumentsAttributes.Count != dataSources.Count)
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

        // For params methods, size the array to max argument count across all [Arguments]
        // rows so that params elements beyond position 0 also get typed casts.
        var arraySize = hasParams ? GetMaxArgumentCount(argumentsAttributes, parameterCount) : parameterCount;

        return ExtractSourceTypesFromArguments(argumentsAttributes, arraySize);
    }

    /// <summary>
    /// Analyzes class-level data source attributes to determine source types for constructor parameters.
    /// </summary>
    public static ITypeSymbol?[]? GetConstructorParameterSourceTypes(INamedTypeSymbol? classType)
    {
        if (classType == null)
        {
            return null;
        }

        var dataSources = new List<AttributeData>();
        var argumentsAttributes = new List<AttributeData>();

        foreach (var attr in classType.GetAttributes())
        {
            if (!DataSourceAttributeHelper.IsDataSourceAttribute(attr.AttributeClass))
            {
                continue;
            }

            dataSources.Add(attr);

            if (IsArgumentsAttribute(attr))
            {
                argumentsAttributes.Add(attr);
            }
        }

        if (dataSources.Count == 0 || argumentsAttributes.Count != dataSources.Count)
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
        var ctorArraySize = ctorHasParams ? GetMaxArgumentCount(argumentsAttributes, ctorParamCount) : ctorParamCount;

        return ExtractSourceTypesFromArguments(argumentsAttributes, ctorArraySize);
    }

    private static int GetMaxArgumentCount(List<AttributeData> argumentsAttributes, int baseCount)
    {
        foreach (var attr in argumentsAttributes)
        {
            var values = GetArgumentValues(attr);
            if (values != null && values.Value.Length > baseCount)
            {
                baseCount = values.Value.Length;
            }
        }

        return baseCount;
    }

    private static ITypeSymbol?[]? ExtractSourceTypesFromArguments(
        List<AttributeData> argumentsAttributes,
        int parameterCount)
    {
        var sourceTypes = new ITypeSymbol?[parameterCount];
        var consistent = new bool[parameterCount];

        for (var i = 0; i < parameterCount; i++)
        {
            consistent[i] = true;
        }

        var firstRow = true;

        foreach (var attr in argumentsAttributes)
        {
            var values = GetArgumentValues(attr);

            if (values == null)
            {
                // Can't extract values from this attribute — fall back entirely
                return null;
            }

            for (var i = 0; i < parameterCount && i < values.Value.Length; i++)
            {
                var tc = values.Value[i];

                // Null literal or error → unknown source type for this position
                if (tc.IsNull || tc.Kind == TypedConstantKind.Error || tc.Type == null)
                {
                    consistent[i] = false;
                    continue;
                }

                if (firstRow || sourceTypes[i] == null)
                {
                    sourceTypes[i] = tc.Type;
                }
                else if (!SymbolEqualityComparer.Default.Equals(sourceTypes[i], tc.Type))
                {
                    consistent[i] = false;
                }
            }

            firstRow = false;
        }

        // Null out inconsistent positions
        for (var i = 0; i < parameterCount; i++)
        {
            if (!consistent[i])
            {
                sourceTypes[i] = null;
            }
        }

        return sourceTypes;
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
        return attr.AttributeClass?.Name is "ArgumentsAttribute";
    }
}
