using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Test metadata for generic methods that registers all known concrete type instantiations
/// and selects the appropriate one at runtime based on argument types.
/// This approach is AOT-compatible as all concrete types are known at compile time.
/// </summary>
public sealed class GenericTestMetadataWithConcreteTypes : TestMetadata
{
    /// <summary>
    /// Dictionary mapping type argument arrays to concrete test metadata instances
    /// </summary>
    public Dictionary<string, TestMetadata> ConcreteInstantiations { get; init; } = new();

    /// <summary>
    /// Factory delegate that creates an ExecutableTest by selecting the appropriate
    /// concrete instantiation based on runtime argument types.
    /// </summary>
    public override Func<ExecutableTestCreationContext, TestMetadata, AbstractExecutableTest> CreateExecutableTestFactory
    {
        get
        {
            return (context, metadata) =>
            {
                var genericMetadata = (GenericTestMetadataWithConcreteTypes)metadata;
                
                // Determine the concrete types from the test arguments
                var inferredTypes = InferTypesFromArguments(context.Arguments, metadata);
                
                if (inferredTypes == null || inferredTypes.Length == 0)
                {
                    throw new InvalidOperationException(
                        $"Could not infer generic type arguments for {metadata.TestMethodName} from arguments");
                }
                
                // Create a key from the inferred types
                var typeKey = string.Join(",", inferredTypes.Select(t => t.FullName ?? t.Name));
                
                // Find the matching concrete instantiation
                if (!genericMetadata.ConcreteInstantiations.TryGetValue(typeKey, out var concreteMetadata))
                {
                    throw new InvalidOperationException(
                        $"No concrete instantiation found for generic method {metadata.TestMethodName} " +
                        $"with type arguments: {typeKey}. Available: {string.Join(", ", genericMetadata.ConcreteInstantiations.Keys)}");
                }
                
                // Use the concrete metadata's factory to create the executable test
                return concreteMetadata.CreateExecutableTestFactory(context, concreteMetadata);
            };
        }
    }
    
    private static Type[]? InferTypesFromArguments(object?[]? arguments, TestMetadata metadata)
    {
        if (arguments == null || arguments.Length == 0)
            return null;
            
        // For methods with generic parameters, infer types from the argument values
        var inferredTypes = new List<Type>();
        
        // Get the method's generic parameters
        var methodInfo = metadata.TestClassType.GetMethod(metadata.TestMethodName);
        if (methodInfo == null || !methodInfo.IsGenericMethodDefinition)
            return null;
            
        var genericParams = methodInfo.GetGenericArguments();
        var methodParams = methodInfo.GetParameters();
        
        // Map argument types to generic parameters
        foreach (var genericParam in genericParams)
        {
            Type? inferredType = null;
            
            // Find which method parameter uses this generic parameter
            for (int i = 0; i < methodParams.Length && i < arguments.Length; i++)
            {
                var paramType = methodParams[i].ParameterType;
                
                // Direct match: parameter type is the generic parameter
                if (paramType.IsGenericParameter && paramType.Name == genericParam.Name)
                {
                    if (arguments[i] != null)
                    {
                        inferredType = arguments[i]!.GetType();
                    }
                    break;
                }
                
                // TODO: Handle more complex cases like IEnumerable<T>, Func<T>, etc.
            }
            
            if (inferredType != null)
            {
                inferredTypes.Add(inferredType);
            }
        }
        
        return inferredTypes.Count > 0 ? inferredTypes.ToArray() : null;
    }
}