using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TUnit.Core.Helpers;

/// <summary>
/// AOT-compatible helper methods for data source processing
/// </summary>
public static class DataSourceHelpers
{
    /// <summary>
    /// Invokes a Func&lt;T&gt; if the value is one, otherwise returns the value as-is.
    /// Note: Most Func invocation should be handled by the source generator at compile time for AOT compatibility.
    /// This method is kept for backward compatibility and edge cases.
    /// </summary>
    public static object? InvokeIfFunc(object? value)
    {
        if (value == null)
        {
            return null;
        }

        if(value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(Func<>))
        {
            return ((Delegate)value).DynamicInvoke();
        }

        return value;
    }

    /// <summary>
    /// Generic version that can be used when the type is known at compile time
    /// </summary>
    public static T InvokeIfFunc<T>(object? value)
    {
        if (value is Func<T> func)
        {
            return func();
        }
        return (T)value!;
    }

    /// <summary>
    /// Handles tuple values for method and class arguments in an AOT-compatible way
    /// Each returned factory invokes the original data source function to ensure fresh instances
    /// </summary>
    public static Func<Task<object?>>[] HandleTupleValue(object? value, bool shouldUnwrap)
    {
        if (!shouldUnwrap || value == null)
        {
            return [() => Task.FromResult<object?>(value)];
        }

        // Use AOT-compatible tuple unwrapping
        var unwrapped = UnwrapTupleAot(value);
        if (unwrapped.Length > 1)
        {
            // Multiple values from tuple - create a factory for each that returns the specific element
            return unwrapped.Select((_, index) => new Func<Task<object?>>(() =>
            {
                var freshUnwrapped = UnwrapTupleAot(value);
                return Task.FromResult<object?>(index < freshUnwrapped.Length ? freshUnwrapped[index] : null);
            })).ToArray();
        }

        // Single value or not a tuple
        return [() => Task.FromResult<object?>(value)];
    }

    /// <summary>
    /// AOT-compatible tuple unwrapping that handles common tuple types without reflection
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2091:Target generic argument does not satisfy 'DynamicallyAccessedMembersAttribute' in target method or type.",
        Justification = "We handle specific known tuple types without reflection")]
    public static object?[] UnwrapTupleAot(object? value)
    {
        if (value == null)
        {
            return [null];
        }

#if NET5_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        // Try to use ITuple interface first for any ValueTuple type (available in .NET Core 3.0+)
        if (value is ITuple tuple)
        {
            var length = tuple.Length;
            var result = new object?[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = tuple[i];
            }
            return result;
        }
#endif

        // Handle common ValueTuple types explicitly as fallback
        switch (value)
        {
            case ValueTuple<object?> vt1:
                return [vt1.Item1];
            case ValueTuple<object?, object?> vt2:
                return [vt2.Item1, vt2.Item2];
            case ValueTuple<object?, object?, object?> vt3:
                return [vt3.Item1, vt3.Item2, vt3.Item3];
            case ValueTuple<object?, object?, object?, object?> vt4:
                return [vt4.Item1, vt4.Item2, vt4.Item3, vt4.Item4];
            case ValueTuple<object?, object?, object?, object?, object?> vt5:
                return [vt5.Item1, vt5.Item2, vt5.Item3, vt5.Item4, vt5.Item5];
            case ValueTuple<object?, object?, object?, object?, object?, object?> vt6:
                return [vt6.Item1, vt6.Item2, vt6.Item3, vt6.Item4, vt6.Item5, vt6.Item6];
            case ValueTuple<object?, object?, object?, object?, object?, object?, object?> vt7:
                return [vt7.Item1, vt7.Item2, vt7.Item3, vt7.Item4, vt7.Item5, vt7.Item6, vt7.Item7];

            // Handle regular Tuple types
            case Tuple<object?> t1:
                return [t1.Item1];
            case Tuple<object?, object?> t2:
                return [t2.Item1, t2.Item2];
            case Tuple<object?, object?, object?> t3:
                return [t3.Item1, t3.Item2, t3.Item3];
            case Tuple<object?, object?, object?, object?> t4:
                return [t4.Item1, t4.Item2, t4.Item3, t4.Item4];
            case Tuple<object?, object?, object?, object?, object?> t5:
                return [t5.Item1, t5.Item2, t5.Item3, t5.Item4, t5.Item5];
            case Tuple<object?, object?, object?, object?, object?, object?> t6:
                return [t6.Item1, t6.Item2, t6.Item3, t6.Item4, t6.Item5, t6.Item6];
            case Tuple<object?, object?, object?, object?, object?, object?, object?> t7:
                return [t7.Item1, t7.Item2, t7.Item3, t7.Item4, t7.Item5, t7.Item6, t7.Item7];

            default:
                // For backward compatibility, fall back to reflection-based approach if available
                // This allows existing code to continue working in non-AOT scenarios
                if (TupleHelper.IsTupleType(value.GetType()))
                {
                    return TupleHelper.UnwrapTuple(value);
                }

                // Not a tuple, return as single-element array
                return [value];
        }
    }

    /// <summary>
    /// Generic tuple unwrapping for when types are known at compile time
    /// </summary>
    public static object?[] UnwrapTuple<T1>(ValueTuple<T1> tuple)
        => [tuple.Item1];

    public static object?[] UnwrapTuple<T1, T2>(ValueTuple<T1, T2> tuple)
        => [tuple.Item1, tuple.Item2];

    public static object?[] UnwrapTuple<T1, T2, T3>(ValueTuple<T1, T2, T3> tuple)
        => [tuple.Item1, tuple.Item2, tuple.Item3];

    public static object?[] UnwrapTuple<T1, T2, T3, T4>(ValueTuple<T1, T2, T3, T4> tuple)
        => [tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4];

    public static object?[] UnwrapTuple<T1, T2, T3, T4, T5>(ValueTuple<T1, T2, T3, T4, T5> tuple)
        => [tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5];

    public static object?[] UnwrapTuple<T1, T2, T3, T4, T5, T6>(ValueTuple<T1, T2, T3, T4, T5, T6> tuple)
        => [tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6];

    public static object?[] UnwrapTuple<T1, T2, T3, T4, T5, T6, T7>(ValueTuple<T1, T2, T3, T4, T5, T6, T7> tuple)
        => [tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7];

    /// <summary>
    /// AOT-compatible data source processor for when the return type is known at compile time
    /// </summary>
    public static async Task<object?> ProcessDataSourceResult<T>(T data)
    {
        if (data == null)
        {
            return null;
        }

        // If it's a Func<TResult>, invoke it first
        var actualData = InvokeIfFunc(data);

        // Initialize the object if it implements IAsyncInitializer
        await ObjectInitializer.InitializeAsync(actualData);

        return actualData;
    }

    /// <summary>
    /// AOT-compatible data source processor for IEnumerable types known at compile time
    /// </summary>
    public static async Task<object?> ProcessEnumerableDataSource<T>(IEnumerable<T> enumerable)
    {
        if (enumerable == null)
        {
            return null;
        }

        var enumerator = enumerable.GetEnumerator();
        if (enumerator.MoveNext())
        {
            var value = enumerator.Current;
            await ObjectInitializer.InitializeAsync(value);
            return value;
        }

        return null;
    }

    /// <summary>
    /// AOT-compatible data source processor that handles any type by checking if it's enumerable
    /// </summary>
    public static async Task<object?> ProcessDataSourceResultGeneric<T>(T data)
    {
        if (data == null)
        {
            return null;
        }

        // If it's a Func<TResult>, invoke it first
        var actualData = InvokeIfFunc(data);

        // Handle IEnumerable types (but not string)
        if (actualData is IEnumerable enumerable and not string)
        {
            var enumerator = enumerable.GetEnumerator();
            if (enumerator.MoveNext())
            {
                var value = enumerator.Current;
                await ObjectInitializer.InitializeAsync(value);
                return value;
            }
            return null;
        }

        // For non-enumerable types, just initialize and return
        await ObjectInitializer.InitializeAsync(actualData);
        return actualData;
    }

    /// <summary>
    /// AOT-compatible method that processes data and returns factories for test data combination
    /// Replaces the complex HandleTupleValue + InvokeIfFunc pattern
    /// Each returned factory invokes the original data source function to ensure fresh instances
    /// </summary>
    public static Func<Task<object?>>[] ProcessTestDataSource<T>(T data, int expectedParameterCount = -1)
    {
        if (data == null)
        {
            return [() => Task.FromResult<object?>(null)];
        }

        // Determine how many factories we need to return based on the data structure
        // We need to evaluate the data once to understand its structure
        var sampleData = InvokeIfFunc(data);

        // Always unwrap tuples - they explicitly represent multiple values
        if (IsTuple(sampleData))
        {
            var unwrapped = UnwrapTupleAot(sampleData);
            // For tuples, return individual factories for each parameter
            if (expectedParameterCount > 0 && unwrapped.Length == expectedParameterCount)
            {
                return unwrapped.Select((_, index) => new Func<Task<object?>>(() =>
                {
                    var freshData = InvokeIfFunc(data);
                    var freshUnwrapped = UnwrapTupleAot(freshData);
                    return Task.FromResult<object?>(index < freshUnwrapped.Length ? freshUnwrapped[index] : null);
                })).ToArray();
            }
            // If parameter count doesn't match, return as single tuple value
            return [() => Task.FromResult<object?>(InvokeIfFunc(data))];
        }

        // For arrays, decide based on expected parameter count
        if (sampleData is object?[] array && expectedParameterCount > 0)
        {
            // If expecting multiple parameters and array length matches, unwrap it
            if (expectedParameterCount > 1 && array.Length == expectedParameterCount)
            {
                return array.Select((_, index) => new Func<Task<object?>>(() =>
                {
                    var freshData = InvokeIfFunc(data);
                    if (freshData is object?[] freshArray && index < freshArray.Length)
                    {
                        return Task.FromResult<object?>(freshArray[index]);
                    }
                    return Task.FromResult<object?>(null);
                })).ToArray();
            }
            // If expecting 1 parameter, keep array as single value
            // Or if array length doesn't match expected count, treat as single value
        }

        // Default: return as single value, invoking the original function each time
        return [() => Task.FromResult<object?>(InvokeIfFunc(data))];
    }


    /// <summary>
    /// AOT-compatible runtime dispatcher for data source property initialization.
    /// This will be populated by the generated DataSourceHelpers class.
    /// </summary>
    private static readonly Dictionary<Type, Func<object, MethodMetadata, string, Task>> PropertyInitializers = new();

    /// <summary>
    /// Register a type-specific property initializer (called by generated code)
    /// </summary>
    public static void RegisterPropertyInitializer<T>(Func<T, MethodMetadata, string, Task> initializer)
    {
        PropertyInitializers[typeof(T)] = (instance, testInfo, sessionId) =>
            initializer((T)instance, testInfo, sessionId);
    }

    /// <summary>
    /// Initialize data source properties on an instance using registered type-specific helpers
    /// </summary>
    public static async Task InitializeDataSourcePropertiesAsync(object? instance, MethodMetadata testInformation, string testSessionId)
    {
        if (instance == null)
        {
            return;
        }

        var instanceType = instance.GetType();

        if (PropertyInitializers.TryGetValue(instanceType, out var initializer))
        {
            await initializer(instance, testInformation, testSessionId);
        }
        // If no initializer is registered, the type has no data source properties
    }

    public static object?[] ToObjectArray(this object? item)
    {
        item = InvokeIfFunc(item);

        if (item is null)
        {
            return [ null ];
        }

        // Check if it's specifically object?[] (not other array types like string[])
        // We need to check the element type because string[] is assignable to object?[] due to covariance
        if(item is object?[] array && item.GetType().GetElementType() == typeof(object))
        {
            return array;
        }

        // Don't treat strings as character arrays
        if (item is string)
        {
            return [item];
        }

        // Check if it's any other kind of array (string[], int[], etc.)
        if (item is Array)
        {
            return [item];
        }

        // Check tuples before IEnumerable because tuples implement IEnumerable
        // but need special unwrapping logic
        if (IsTuple(item))
        {
            return UnwrapTupleAot(item);
        }

        // Don't expand IEnumerable - test methods expect the IEnumerable itself as a parameter
        // Only arrays and tuples are expanded (handled above)
        return [item];
    }

    public static bool IsTuple(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

#if NET5_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
    // Fast path for modern .NET: ITuple covers all tuple types
    return obj is System.Runtime.CompilerServices.ITuple;
#else
        // Fallback: check for known tuple types
        var type = obj.GetType();
        if (!type.IsGenericType)
        {
            return false;
        }

        var genericType = type.GetGenericTypeDefinition();
        return genericType == typeof(ValueTuple<>) ||
            genericType == typeof(ValueTuple<,>) ||
            genericType == typeof(ValueTuple<,,>) ||
            genericType == typeof(ValueTuple<,,,>) ||
            genericType == typeof(ValueTuple<,,,,>) ||
            genericType == typeof(ValueTuple<,,,,,>) ||
            genericType == typeof(ValueTuple<,,,,,,>) ||
            genericType == typeof(Tuple<>) ||
            genericType == typeof(Tuple<,>) ||
            genericType == typeof(Tuple<,,>) ||
            genericType == typeof(Tuple<,,,>) ||
            genericType == typeof(Tuple<,,,,>) ||
            genericType == typeof(Tuple<,,,,,>) ||
            genericType == typeof(Tuple<,,,,,,>);
#endif
    }
}
