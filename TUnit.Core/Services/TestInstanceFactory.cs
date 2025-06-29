using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Services;

/// <summary>
/// Default implementation of test instance factory using simple reflection.
/// No expression compilation for better maintainability.
/// </summary>
[RequiresDynamicCode("Test instance creation requires runtime type instantiation and method invocation")]
[RequiresUnreferencedCode("Test instance creation may require types and members not preserved by trimming")]
public class TestInstanceFactory : ITestInstanceFactory
{
    /// <inheritdoc />
    public Task<object> CreateInstanceAsync(Type type, object?[] args)
    {
        try
        {
            // Find a constructor that matches the argument count
            var constructor = FindBestConstructor(type, args);

            if (constructor == null)
            {
                throw new InvalidOperationException(
                    $"No suitable constructor found for type {type.FullName} with {args.Length} arguments.");
            }

            // Create instance using the constructor
            var instance = constructor.Invoke(args);
            return Task.FromResult(instance);
        }
        catch (TargetInvocationException ex)
        {
            // Unwrap the inner exception
            throw ex.InnerException ?? ex;
        }
    }

    /// <inheritdoc />
    public async Task<object?> InvokeMethodAsync(object instance, MethodInfo method, object?[] args)
    {
        try
        {
            // Handle generic methods
            var methodToInvoke = method;
            if (method.IsGenericMethodDefinition)
            {
                var genericTypes = InferGenericTypes(method, args);
                methodToInvoke = method.MakeGenericMethod(genericTypes);
            }

            // Invoke the method
            var result = methodToInvoke.Invoke(instance, args);

            // Handle async methods
            if (result is Task task)
            {
                await task.ConfigureAwait(false);

                // Get the result if it's a Task<T>
                var taskType = task.GetType();
                if (taskType.IsGenericType && taskType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var resultProperty = taskType.GetProperty("Result");
                    return resultProperty?.GetValue(task);
                }

                return null;
            }

            return result;
        }
        catch (TargetInvocationException ex)
        {
            // Unwrap the inner exception
            throw ex.InnerException ?? ex;
        }
    }

    /// <inheritdoc />
    public Task SetPropertyAsync(object instance, PropertyInfo property, object? value)
    {
        try
        {
            property.SetValue(instance, value);
            return Task.CompletedTask;
        }
        catch (TargetInvocationException ex)
        {
            // Unwrap the inner exception
            throw ex.InnerException ?? ex;
        }
    }

    private static ConstructorInfo? FindBestConstructor(Type type, object?[] args)
    {
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        // First try to find exact match
        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length != args.Length)
            {
                continue;
            }

            // Check if all arguments are compatible
            var isMatch = true;
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                var argValue = args[i];

                if (argValue == null)
                {
                    // Null is compatible with reference types and nullable value types
                    if (paramType.IsValueType && Nullable.GetUnderlyingType(paramType) == null)
                    {
                        isMatch = false;
                        break;
                    }
                }
                else if (!paramType.IsAssignableFrom(argValue.GetType()))
                {
                    isMatch = false;
                    break;
                }
            }

            if (isMatch)
            {
                return constructor;
            }
        }

        // If no exact match, try to find a constructor with the same parameter count
        return constructors.FirstOrDefault(c => c.GetParameters().Length == args.Length);
    }

    private static Type[] InferGenericTypes(MethodInfo method, object?[] args)
    {
        var genericParameters = method.GetGenericArguments();
        var inferredTypes = new Type[genericParameters.Length];
        var parameters = method.GetParameters();

        // Simple type inference based on arguments
        for (int i = 0; i < genericParameters.Length; i++)
        {
            var genericParam = genericParameters[i];

            // Try to infer from method parameters
            for (int j = 0; j < parameters.Length && j < args.Length; j++)
            {
                if (args[j] != null)
                {
                    var paramType = parameters[j].ParameterType;
                    if (paramType.IsGenericType && paramType.ContainsGenericParameters)
                    {
                        // Simple case: direct generic parameter
                        if (paramType == genericParam)
                        {
                            inferredTypes[i] = args[j]!.GetType();
                            break;
                        }

                        // Handle generic collections like IEnumerable<T>
                        if (paramType.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                            paramType.GetGenericArguments()[0] == genericParam)
                        {
                            var argType = args[j]!.GetType();
                            var elementType = GetEnumerableElementType(argType);
                            if (elementType != null)
                            {
                                inferredTypes[i] = elementType;
                                break;
                            }
                        }
                    }
                }
            }

            // If we couldn't infer, use object as fallback
            inferredTypes[i] ??= typeof(object);
        }

        return inferredTypes;
    }

    private static Type? GetEnumerableElementType(Type type)
    {
        // Check if it implements IEnumerable<T>
        var enumerableInterface = type
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerableInterface?.GetGenericArguments()[0];
    }
}
