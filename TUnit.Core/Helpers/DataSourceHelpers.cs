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
        // This method is now mostly deprecated as Func invocation
        // is handled by the source generator at compile time
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
    /// </summary>
    public static Func<Task<object?>>[] HandleTupleValue(object? value, bool shouldUnwrap)
    {
        if (!shouldUnwrap || value == null)
        {
            return new[] { () => Task.FromResult<object?>(value) };
        }

        // Use AOT-compatible tuple unwrapping
        var unwrapped = UnwrapTupleAot(value);
        if (unwrapped.Length > 1)
        {
            // Multiple values from tuple - create a factory for each
            return unwrapped.Select(v => new Func<Task<object?>>(() => Task.FromResult<object?>(v))).ToArray();
        }

        // Single value or not a tuple
        return new[] { () => Task.FromResult<object?>(value) };
    }

    /// <summary>
    /// AOT-compatible tuple unwrapping that handles common tuple types without reflection
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2091:Target generic argument does not satisfy 'DynamicallyAccessedMembersAttribute' in target method or type.",
        Justification = "We handle specific known tuple types without reflection")]
    public static object?[] UnwrapTupleAot(object? value)
    {
        if (value == null) return new object?[] { null };

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
                return new[] { vt1.Item1 };
            case ValueTuple<object?, object?> vt2:
                return new[] { vt2.Item1, vt2.Item2 };
            case ValueTuple<object?, object?, object?> vt3:
                return new[] { vt3.Item1, vt3.Item2, vt3.Item3 };
            case ValueTuple<object?, object?, object?, object?> vt4:
                return new[] { vt4.Item1, vt4.Item2, vt4.Item3, vt4.Item4 };
            case ValueTuple<object?, object?, object?, object?, object?> vt5:
                return new[] { vt5.Item1, vt5.Item2, vt5.Item3, vt5.Item4, vt5.Item5 };
            case ValueTuple<object?, object?, object?, object?, object?, object?> vt6:
                return new[] { vt6.Item1, vt6.Item2, vt6.Item3, vt6.Item4, vt6.Item5, vt6.Item6 };
            case ValueTuple<object?, object?, object?, object?, object?, object?, object?> vt7:
                return new[] { vt7.Item1, vt7.Item2, vt7.Item3, vt7.Item4, vt7.Item5, vt7.Item6, vt7.Item7 };

            // Handle regular Tuple types
            case Tuple<object?> t1:
                return new[] { t1.Item1 };
            case Tuple<object?, object?> t2:
                return new[] { t2.Item1, t2.Item2 };
            case Tuple<object?, object?, object?> t3:
                return new[] { t3.Item1, t3.Item2, t3.Item3 };
            case Tuple<object?, object?, object?, object?> t4:
                return new[] { t4.Item1, t4.Item2, t4.Item3, t4.Item4 };
            case Tuple<object?, object?, object?, object?, object?> t5:
                return new[] { t5.Item1, t5.Item2, t5.Item3, t5.Item4, t5.Item5 };
            case Tuple<object?, object?, object?, object?, object?, object?> t6:
                return new[] { t6.Item1, t6.Item2, t6.Item3, t6.Item4, t6.Item5, t6.Item6 };
            case Tuple<object?, object?, object?, object?, object?, object?, object?> t7:
                return new[] { t7.Item1, t7.Item2, t7.Item3, t7.Item4, t7.Item5, t7.Item6, t7.Item7 };

            default:
                // For backward compatibility, fall back to reflection-based approach if available
                // This allows existing code to continue working in non-AOT scenarios
                if (TupleHelper.IsTupleType(value.GetType()))
                {
                    return TupleHelper.UnwrapTuple(value);
                }

                // Not a tuple, return as single-element array
                return new object?[] { value };
        }
    }

    /// <summary>
    /// Generic tuple unwrapping for when types are known at compile time
    /// </summary>
    public static object?[] UnwrapTuple<T1>(ValueTuple<T1> tuple)
        => new object?[] { tuple.Item1 };

    public static object?[] UnwrapTuple<T1, T2>(ValueTuple<T1, T2> tuple)
        => new object?[] { tuple.Item1, tuple.Item2 };

    public static object?[] UnwrapTuple<T1, T2, T3>(ValueTuple<T1, T2, T3> tuple)
        => new object?[] { tuple.Item1, tuple.Item2, tuple.Item3 };

    public static object?[] UnwrapTuple<T1, T2, T3, T4>(ValueTuple<T1, T2, T3, T4> tuple)
        => new object?[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4 };

    public static object?[] UnwrapTuple<T1, T2, T3, T4, T5>(ValueTuple<T1, T2, T3, T4, T5> tuple)
        => new object?[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5 };

    public static object?[] UnwrapTuple<T1, T2, T3, T4, T5, T6>(ValueTuple<T1, T2, T3, T4, T5, T6> tuple)
        => new object?[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6 };

    public static object?[] UnwrapTuple<T1, T2, T3, T4, T5, T6, T7>(ValueTuple<T1, T2, T3, T4, T5, T6, T7> tuple)
        => new object?[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7 };

    /// <summary>
    /// AOT-compatible data source processor for when the return type is known at compile time
    /// </summary>
    public static async Task<object?> ProcessDataSourceResult<T>(T data)
    {
        if (data == null)
            return null;

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
            return null;

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
            return null;

        // If it's a Func<TResult>, invoke it first
        var actualData = InvokeIfFunc(data);

        // Handle IEnumerable types (but not string)
        if (actualData is IEnumerable enumerable && actualData is not string)
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
    /// </summary>
    public static Func<Task<object?>>[] ProcessTestDataSource<T>(T data, int expectedParameterCount = -1)
    {
        if (data == null)
        {
            return new[] { () => Task.FromResult<object?>(null) };
        }

        // Note: Func invocation is now handled by the source generator at compile time
        // for better AOT compatibility

        // Always unwrap tuples - they explicitly represent multiple values
        if (IsTuple(data))
        {
            var unwrapped = UnwrapTupleAot(data);
            return unwrapped.Select(v => new Func<Task<object?>>(() => Task.FromResult(v))).ToArray();
        }
        
        // For arrays, decide based on expected parameter count
        if (data is object?[] array && expectedParameterCount > 0)
        {
            // If expecting multiple parameters and array length matches, unwrap it
            if (expectedParameterCount > 1 && array.Length == expectedParameterCount)
            {
                return array.Select(v => new Func<Task<object?>>(() => Task.FromResult(v))).ToArray();
            }
            // If expecting 1 parameter, keep array as single value
            // Or if array length doesn't match expected count, treat as single value
        }
        
        // Default: return as single value
        return new[] { () => Task.FromResult<object?>(data) };
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
        if (instance == null) return;

        var instanceType = instance.GetType();

        if (PropertyInitializers.TryGetValue(instanceType, out var initializer))
        {
            await initializer(instance, testInformation, testSessionId);
        }
        // If no initializer is registered, the type has no data source properties
    }

    public static object?[] ToObjectArray(this object? item)
    {
        if (item is null)
        {
            return [ null ];
        }

        if(item is object?[] array)
        {
            return array;
        }

        // Don't treat strings as character arrays
        if (item is string)
        {
            return [item];
        }

        if (item is IEnumerable enumerable)
        {
            return enumerable.Cast<object?>().ToArray();
        }

        if (IsTuple(item))
        {
            return UnwrapTupleAot(item);
        }

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
