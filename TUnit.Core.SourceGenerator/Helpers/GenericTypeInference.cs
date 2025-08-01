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

        // Try to infer from Matrix parameter attributes
        inferredTypes = TryInferFromMatrixParameters(method);
        if (inferredTypes != null)
        {
            return inferredTypes;
        }

        // Try to infer from MethodDataSource
        inferredTypes = TryInferFromMethodDataSource(method, attributes);
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

    private static ImmutableArray<ITypeSymbol>? TryInferFromMatrixParameters(IMethodSymbol method)
    {
        var inferredTypes = new List<ITypeSymbol>();

        // Look at each parameter to see if it has Matrix<T> attributes
        foreach (var parameter in method.Parameters)
        {
            if (parameter.Type is ITypeParameterSymbol typeParam)
            {
                // Check if this parameter has a Matrix<T> attribute
                foreach (var attr in parameter.GetAttributes())
                {
                    if (attr.AttributeClass?.Name == "MatrixAttribute" && 
                        attr.AttributeClass.IsGenericType &&
                        attr.AttributeClass.TypeArguments.Length > 0)
                    {
                        // Get the type argument from Matrix<T>
                        var matrixType = attr.AttributeClass.TypeArguments[0];
                        
                        // Find the index of this type parameter
                        var typeParamIndex = -1;
                        for (int i = 0; i < method.TypeParameters.Length; i++)
                        {
                            if (method.TypeParameters[i].Name == typeParam.Name)
                            {
                                typeParamIndex = i;
                                break;
                            }
                        }

                        if (typeParamIndex >= 0)
                        {
                            // Make sure we have enough slots
                            while (inferredTypes.Count <= typeParamIndex)
                            {
                                inferredTypes.Add(null!);
                            }
                            inferredTypes[typeParamIndex] = matrixType;
                        }
                    }
                }
            }
        }

        // Remove any null entries and check if we have all types
        inferredTypes.RemoveAll(t => t == null);
        
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

    private static ImmutableArray<ITypeSymbol>? TryInferFromMethodDataSource(IMethodSymbol testMethod, ImmutableArray<AttributeData> attributes)
    {
        var methodDataSourceAttributes = attributes
            .Where(a => a.AttributeClass?.Name == "MethodDataSourceAttribute")
            .ToList();

        if (!methodDataSourceAttributes.Any())
            return null;

        foreach (var attr in methodDataSourceAttributes)
        {
            if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string methodName)
            {
                // Find the data source method
                var dataSourceMethod = testMethod.ContainingType.GetMembers(methodName)
                    .OfType<IMethodSymbol>()
                    .FirstOrDefault();

                if (dataSourceMethod != null)
                {
                    // Analyze the return type to extract generic types
                    var returnType = dataSourceMethod.ReturnType;
                    
                    // Handle IEnumerable<Func<(...)>>
                    if (returnType is INamedTypeSymbol namedType && 
                        namedType.IsGenericType &&
                        namedType.TypeArguments.Length > 0)
                    {
                        var funcType = namedType.TypeArguments[0];
                        if (funcType is INamedTypeSymbol funcNamedType && 
                            funcNamedType.Name == "Func" &&
                            funcNamedType.TypeArguments.Length > 0)
                        {
                            var tupleType = funcNamedType.TypeArguments[0];
                            if (tupleType is INamedTypeSymbol tupleNamedType && tupleNamedType.IsTupleType)
                            {
                                // Extract types from tuple elements
                                var inferredTypes = InferTypesFromTupleElements(testMethod, tupleNamedType);
                                if (inferredTypes != null)
                                    return inferredTypes;
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    private static ImmutableArray<ITypeSymbol>? InferTypesFromTupleElements(IMethodSymbol testMethod, INamedTypeSymbol tupleType)
    {
        var inferredTypes = new ITypeSymbol[testMethod.TypeParameters.Length];
        var tupleElements = tupleType.TupleElements;

        // Map tuple elements to method parameters
        for (int i = 0; i < testMethod.Parameters.Length && i < tupleElements.Length; i++)
        {
            var parameter = testMethod.Parameters[i];
            var tupleElement = tupleElements[i];

            if (parameter.Type is ITypeParameterSymbol typeParam)
            {
                // Find the index of this type parameter
                var typeParamIndex = -1;
                for (int j = 0; j < testMethod.TypeParameters.Length; j++)
                {
                    if (testMethod.TypeParameters[j].Name == typeParam.Name)
                    {
                        typeParamIndex = j;
                        break;
                    }
                }

                if (typeParamIndex >= 0)
                {
                    // For generic types like IEnumerable<T>, extract T
                    var elementType = tupleElement.Type;
                    if (elementType is INamedTypeSymbol namedElementType && 
                        namedElementType.IsGenericType &&
                        namedElementType.TypeArguments.Length > 0)
                    {
                        // For IEnumerable<int>, we want int
                        inferredTypes[typeParamIndex] = namedElementType.TypeArguments[0];
                    }
                    else
                    {
                        // For direct types
                        inferredTypes[typeParamIndex] = elementType;
                    }
                }
            }
            else if (parameter.Type is INamedTypeSymbol paramNamedType && paramNamedType.IsGenericType)
            {
                // Handle complex generic parameters like Func<TSource, TKey>
                // This is more complex and would need deeper analysis
                var tupleElementType = tupleElement.Type;
                if (tupleElementType is INamedTypeSymbol funcType && funcType.Name == "Func")
                {
                    // Match type arguments between parameter type and tuple element type
                    for (int j = 0; j < funcType.TypeArguments.Length && j < paramNamedType.TypeArguments.Length; j++)
                    {
                        var paramTypeArg = paramNamedType.TypeArguments[j];
                        if (paramTypeArg is ITypeParameterSymbol funcTypeParam)
                        {
                            var typeParamIndex = -1;
                            for (int k = 0; k < testMethod.TypeParameters.Length; k++)
                            {
                                if (testMethod.TypeParameters[k].Name == funcTypeParam.Name)
                                {
                                    typeParamIndex = k;
                                    break;
                                }
                            }

                            if (typeParamIndex >= 0)
                            {
                                inferredTypes[typeParamIndex] = funcType.TypeArguments[j];
                            }
                        }
                    }
                }
            }
        }

        // Check if we have all required types
        if (inferredTypes.All(t => t != null))
        {
            return inferredTypes.ToImmutableArray();
        }

        return null;
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

        // For Matrix parameter attributes
        var matrixTypes = TryInferFromMatrixParameters(method);
        if (matrixTypes != null && !combinations.Any(c => TypeArraysEqual(c, matrixTypes.Value)))
        {
            combinations.Add(matrixTypes.Value);
        }

        // For MethodDataSource attributes
        var methodDataSourceTypes = TryInferFromMethodDataSource(method, attributes);
        if (methodDataSourceTypes != null && !combinations.Any(c => TypeArraysEqual(c, methodDataSourceTypes.Value)))
        {
            combinations.Add(methodDataSourceTypes.Value);
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