using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Test metadata implementation for generic types that need runtime type resolution
/// </summary>
public sealed class GenericTestMetadata : TestMetadata
{
    /// <summary>
    /// Dictionary mapping type argument arrays to concrete test metadata instances.
    /// When populated, this enables AOT-compatible execution by avoiding runtime type resolution.
    /// </summary>
    public Dictionary<string, TestMetadata>? ConcreteInstantiations { get; init; }

    /// <summary>
    /// Factory delegate that creates an ExecutableTest for this metadata.
    /// Uses ConcreteInstantiations when available (AOT-compatible), otherwise falls back to reflection.
    /// </summary>
    public override Func<ExecutableTestCreationContext, TestMetadata, AbstractExecutableTest> CreateExecutableTestFactory
    {
        get
        {
            return (context, metadata) =>
            {
                var genericMetadata = (GenericTestMetadata)metadata;

                // If we have concrete instantiations, try to use them (AOT-compatible path)
                if (genericMetadata.ConcreteInstantiations?.Count > 0)
                {
                    // Determine the concrete types from the test arguments
                    var inferredTypes = InferTypesFromArguments(context.Arguments, metadata);

                    if (inferredTypes is { Length: > 0 })
                    {
                        // Create a key from the inferred types - must match source generator format
                        var typeKey = string.Join(",", inferredTypes.Select(t => t.FullName ?? t.Name));

                        // Find the matching concrete instantiation
                        if (genericMetadata.ConcreteInstantiations.TryGetValue(typeKey, out var concreteMetadata))
                        {
                            // Use the concrete metadata's factory to create the executable test
                            return concreteMetadata.CreateExecutableTestFactory(context, concreteMetadata);
                        }
                    }

                    // If we couldn't find a match but have instantiations, throw an error
                    var availableKeys = string.Join(", ", genericMetadata.ConcreteInstantiations.Keys);
                    throw new InvalidOperationException(
                        $"No concrete instantiation found for generic method {metadata.TestMethodName} " +
                        $"with type arguments: {(inferredTypes?.Length > 0 ? string.Join(",", inferredTypes.Select(t => t.FullName ?? t.Name)) : "unknown")}. " +
                        $"Available: {availableKeys}");
                }

                // Fall back to runtime resolution (existing logic)
                Func<TestContext, Task<object>> createInstance = async (testContext) =>
                {
                    // Try to create instance with ClassConstructor attribute
                    var attributes = metadata.AttributeFactory();
                    var classInstance = await ClassConstructorHelper.TryCreateInstanceWithClassConstructor(
                        attributes,
                        TestClassType,
                        metadata.TestSessionId,
                        testContext);

                    if (classInstance != null)
                    {
                        // Property injection is handled by SingleTestExecutor after instance creation
                        return classInstance;
                    }

                    // Fall back to default instance factory
                    if (InstanceFactory == null)
                    {
                        throw new InvalidOperationException($"No instance factory for {TestClassType.Name}");
                    }

                    Type[] typeArgs;

                    // First, check if we have resolved class generic arguments from the context
                    // This happens when type inference was done in TestBuilder
                    if (context.ResolvedClassGenericArguments.Length > 0)
                    {
                        typeArgs = context.ResolvedClassGenericArguments;
                    }
                    // Fall back to inferring from constructor arguments if available
                    else if (TestClassType.IsGenericTypeDefinition && context.ClassArguments is { Length: > 0 })
                    {
                        // Infer type arguments from the constructor argument values
                        var genericParams = TestClassType.GetGenericArguments();
                        typeArgs = new Type[genericParams.Length];

                        // For single generic parameter, use the first argument's type
                        if (genericParams.Length == 1 && context.ClassArguments.Length >= 1)
                        {
                            typeArgs[0] = context.ClassArguments[0]?.GetType() ?? typeof(object);
                        }
                        else
                        {
                            // For multiple generic parameters, try to match one-to-one
                            for (var i = 0; i < genericParams.Length; i++)
                            {
                                if (i < context.ClassArguments.Length && context.ClassArguments[i] != null)
                                {
                                    typeArgs[i] = context.ClassArguments[i]!.GetType();
                                }
                                else
                                {
                                    typeArgs[i] = typeof(object);
                                }
                            }
                        }
                    }
                    else
                    {
                        typeArgs = testContext.TestDetails.TestClassArguments?.OfType<Type>().ToArray() ?? Type.EmptyTypes;
                    }

                    var instance = InstanceFactory(typeArgs, context.ClassArguments ?? Array.Empty<object?>());

                    // Property injection is handled by SingleTestExecutor after instance creation
                    return instance;
                };

                Func<object, object?[], TestContext, CancellationToken, Task> invokeTest = async (instance, args, testContext, cancellationToken) =>
                {
                    if (TestInvoker == null)
                    {
                        throw new InvalidOperationException($"No test invoker for {TestMethodName}");
                    }

                    // Determine if the test method has a CancellationToken parameter
                    var parameterTypes = metadata.MethodMetadata.Parameters.Select(p => p.Type).ToArray();
                    var hasCancellationToken = parameterTypes.Any(t => t == typeof(CancellationToken));

                    if (hasCancellationToken)
                    {
                        var cancellationTokenIndex = Array.IndexOf(parameterTypes, typeof(CancellationToken));

                        // Insert CancellationToken at the correct position
                        var argsWithToken = new object?[args.Length + 1];
                        var argIndex = 0;

                        for (var i = 0; i < argsWithToken.Length; i++)
                        {
                            if (i == cancellationTokenIndex)
                            {
                                argsWithToken[i] = cancellationToken;
                            }
                            else if (argIndex < args.Length)
                            {
                                argsWithToken[i] = args[argIndex++];
                            }
                        }

                        await TestInvoker(instance, argsWithToken);
                    }
                    else
                    {
                        await TestInvoker(instance, args);
                    }
                };

                return new ExecutableTest(createInstance, invokeTest)
                {
                    TestId = context.TestId,
                    Metadata = metadata,
                    Arguments = context.Arguments,
                    ClassArguments = context.ClassArguments,
                    Context = context.Context
                };
            };
        }
    }

    private static Type[]? InferTypesFromArguments(object?[]? arguments, TestMetadata metadata)
    {
        if (arguments == null || arguments.Length == 0)
        {
            return null;
        }

        // For methods with generic parameters, infer types from the argument values
        var inferredTypes = new List<Type>();

        // Get the method's generic parameters
        var methodInfo = metadata.TestClassType.GetMethod(metadata.TestMethodName);
        if (methodInfo == null || !methodInfo.IsGenericMethodDefinition)
        {
            return null;
        }

        var genericParams = methodInfo.GetGenericArguments();
        var methodParams = methodInfo.GetParameters();

        // Map argument types to generic parameters
        foreach (var genericParam in genericParams)
        {
            Type? inferredType = null;

            // Find which method parameter uses this generic parameter
            for (var i = 0; i < methodParams.Length && i < arguments.Length; i++)
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

                // Handle generic types like IEnumerable<T>, Func<T>, etc.
                if (paramType.IsGenericType && arguments[i] != null)
                {
                    var inferredFromGeneric = InferTypeFromGenericParameter(paramType, arguments[i]!.GetType(), genericParam);
                    if (inferredFromGeneric != null)
                    {
                        inferredType = inferredFromGeneric;
                        break;
                    }
                }
            }

            if (inferredType != null)
            {
                inferredTypes.Add(inferredType);
            }
        }

        return inferredTypes.Count > 0 ? inferredTypes.ToArray() : null;
    }

    private static Type? InferTypeFromGenericParameter(Type paramType, Type argumentType, Type genericParam)
    {
        // Handle IEnumerable<T>
        if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var typeArg = paramType.GetGenericArguments()[0];
            if (typeArg.IsGenericParameter && typeArg.Name == genericParam.Name)
            {
                // Try to find IEnumerable<T> in the argument type
                if (argumentType.IsGenericType && argumentType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return argumentType.GetGenericArguments()[0];
                }

                // For types that implement IEnumerable<T> but aren't directly IEnumerable<T>
                // we can't easily determine the type parameter at runtime in an AOT-compatible way
                // The source generator should handle this at compile time instead
                return null;
            }
        }

        // Handle Func<T1, T2, ...>
        if (paramType.IsGenericType && paramType.Name.StartsWith("Func`"))
        {
            var paramTypeArgs = paramType.GetGenericArguments();
            var actualFuncType = argumentType;

            // If the argument is not directly a Func, check if it implements one
            if (!argumentType.IsGenericType || !argumentType.Name.StartsWith("Func`"))
            {
                // Could be a lambda or method group - can't easily determine types at runtime
                return null;
            }

            var actualTypeArgs = actualFuncType.GetGenericArguments();

            // Find which position contains our generic parameter
            for (var i = 0; i < paramTypeArgs.Length && i < actualTypeArgs.Length; i++)
            {
                if (paramTypeArgs[i].IsGenericParameter && paramTypeArgs[i].Name == genericParam.Name)
                {
                    return actualTypeArgs[i];
                }
            }
        }

        // Handle other generic types recursively
        if (paramType.IsGenericType && argumentType.IsGenericType)
        {
            var paramGenericDef = paramType.GetGenericTypeDefinition();
            var argGenericDef = argumentType.IsGenericTypeDefinition ? argumentType : argumentType.GetGenericTypeDefinition();

            if (paramGenericDef == argGenericDef)
            {
                var paramTypeArgs = paramType.GetGenericArguments();
                var actualTypeArgs = argumentType.GetGenericArguments();

                for (var i = 0; i < paramTypeArgs.Length && i < actualTypeArgs.Length; i++)
                {
                    if (paramTypeArgs[i].IsGenericParameter && paramTypeArgs[i].Name == genericParam.Name)
                    {
                        return actualTypeArgs[i];
                    }

                    // Recursive check for nested generics
                    if (paramTypeArgs[i].IsGenericType)
                    {
                        var result = InferTypeFromGenericParameter(paramTypeArgs[i], actualTypeArgs[i], genericParam);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
            }
        }

        return null;
    }
}
