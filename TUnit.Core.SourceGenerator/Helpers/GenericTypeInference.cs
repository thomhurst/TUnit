using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

namespace TUnit.Core.SourceGenerator.Helpers;

/// <summary>
/// Provides generic type inference capabilities for test methods with data sources
/// </summary>
internal static class GenericTypeInference
{
    /// <summary>
    /// Infers generic type arguments for a test method based on its data source attributes
    /// </summary>
    public static ImmutableArray<ITypeSymbol>? InferGenericTypes(IMethodSymbol method, ImmutableArray<AttributeData> attributes)
    {
        if (!method.IsGenericMethod || method.TypeParameters.Length == 0)
        {
            return null;
        }

        // Try to infer from typed data sources first
        var inferredTypes = TryInferFromTypedDataSources(method, attributes);
        if (inferredTypes != null)
        {
            return inferredTypes;
        }

        // Try to infer from Arguments attributes
        inferredTypes = TryInferFromArguments(method, attributes);
        if (inferredTypes != null)
        {
            return inferredTypes;
        }

        return null;
    }

    private static ImmutableArray<ITypeSymbol>? TryInferFromTypedDataSources(IMethodSymbol method, ImmutableArray<AttributeData> attributes)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass == null)
                continue;

            // Check if this is a typed data source (inherits from AsyncDataSourceGeneratorAttribute<T> or DataSourceGeneratorAttribute<T>)
            var baseType = GetTypedDataSourceBase(attribute.AttributeClass);
            if (baseType != null && baseType.TypeArguments.Length > 0)
            {
                // For single type parameter methods, use the first type argument
                if (method.TypeParameters.Length == 1)
                {
                    return ImmutableArray.Create(baseType.TypeArguments[0]);
                }

                // For multiple type parameters, match by parameter position if possible
                if (baseType.TypeArguments.Length >= method.TypeParameters.Length)
                {
                    return baseType.TypeArguments.Take(method.TypeParameters.Length).ToImmutableArray();
                }
            }
        }

        return null;
    }

    private static INamedTypeSymbol? GetTypedDataSourceBase(INamedTypeSymbol attributeClass)
    {
        var current = attributeClass;
        while (current != null)
        {
            // Check if it's a generic base class
            if (current.IsGenericType)
            {
                var name = current.Name;
                if (name.Contains("DataSourceGeneratorAttribute") || 
                    name.Contains("AsyncDataSourceGeneratorAttribute"))
                {
                    return current;
                }
            }

            current = current.BaseType;
        }

        return null;
    }

    private static ImmutableArray<ITypeSymbol>? TryInferFromArguments(IMethodSymbol method, ImmutableArray<AttributeData> attributes)
    {
        var argumentsAttributes = attributes
            .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute")
            .ToList();

        if (argumentsAttributes.Count == 0)
            return null;

        // Get the first Arguments attribute to infer types
        var firstArgs = argumentsAttributes[0];
        if (firstArgs.ConstructorArguments.Length == 0)
            return null;

        var inferredTypes = new List<ITypeSymbol>();

        // Match type parameters with method parameters
        for (int i = 0; i < method.TypeParameters.Length && i < method.Parameters.Length; i++)
        {
            var parameter = method.Parameters[i];
            
            // Check if this parameter uses a type parameter
            if (parameter.Type is ITypeParameterSymbol typeParam)
            {
                // Get the corresponding argument value from the attribute
                if (i < firstArgs.ConstructorArguments.Length)
                {
                    var argValue = firstArgs.ConstructorArguments[i];
                    var inferredType = InferTypeFromValue(argValue);
                    
                    if (inferredType != null)
                    {
                        inferredTypes.Add(inferredType);
                    }
                }
            }
        }

        return inferredTypes.Count == method.TypeParameters.Length 
            ? inferredTypes.ToImmutableArray() 
            : null;
    }

    private static ITypeSymbol? InferTypeFromValue(TypedConstant value)
    {
        if (value.IsNull)
            return null;

        // The type of the constant value tells us what T should be
        return value.Type;
    }

    /// <summary>
    /// Gets all unique generic type combinations for a method based on its data sources
    /// </summary>
    public static ImmutableArray<ImmutableArray<ITypeSymbol>> GetAllGenericTypeCombinations(
        IMethodSymbol method, 
        ImmutableArray<AttributeData> attributes)
    {
        var combinations = new List<ImmutableArray<ITypeSymbol>>();

        // For Arguments attributes, each one might produce a different type combination
        var argumentsAttributes = attributes
            .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute")
            .ToList();

        foreach (var args in argumentsAttributes)
        {
            var types = InferTypesFromSingleArguments(method, args);
            if (types != null && !combinations.Any(c => TypeArraysEqual(c, types.Value)))
            {
                combinations.Add(types.Value);
            }
        }

        // For typed data sources, we typically get one type combination
        var typedSourceTypes = TryInferFromTypedDataSources(method, attributes);
        if (typedSourceTypes != null && !combinations.Any(c => TypeArraysEqual(c, typedSourceTypes.Value)))
        {
            combinations.Add(typedSourceTypes.Value);
        }

        return combinations.ToImmutableArray();
    }

    private static ImmutableArray<ITypeSymbol>? InferTypesFromSingleArguments(IMethodSymbol method, AttributeData args)
    {
        if (!method.IsGenericMethod || args.ConstructorArguments.Length == 0)
            return null;

        var inferredTypes = new List<ITypeSymbol>();

        for (int i = 0; i < method.TypeParameters.Length && i < method.Parameters.Length; i++)
        {
            var parameter = method.Parameters[i];
            
            if (parameter.Type is ITypeParameterSymbol)
            {
                if (i < args.ConstructorArguments.Length)
                {
                    var argValue = args.ConstructorArguments[i];
                    var inferredType = InferTypeFromValue(argValue);
                    
                    if (inferredType != null)
                    {
                        inferredTypes.Add(inferredType);
                    }
                }
            }
        }

        return inferredTypes.Count == method.TypeParameters.Length 
            ? inferredTypes.ToImmutableArray() 
            : null;
    }

    private static bool TypeArraysEqual(ImmutableArray<ITypeSymbol> a, ImmutableArray<ITypeSymbol> b)
    {
        if (a.Length != b.Length)
            return false;

        for (int i = 0; i < a.Length; i++)
        {
            if (!SymbolEqualityComparer.Default.Equals(a[i], b[i]))
                return false;
        }

        return true;
    }
}