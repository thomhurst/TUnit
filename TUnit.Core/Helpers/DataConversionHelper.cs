using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TUnit.Core.Helpers;

/// <summary>
/// Helper methods for converting data source results to the standard object?[] format
/// </summary>
public static class DataConversionHelper
{
    /// <summary>
    /// Converts various data source return types to IEnumerable<object?[]>
    /// </summary>
    public static IEnumerable<object?[]> ConvertToObjectArrays(object? data)
    {
        switch (data)
        {
            case null:
                yield return new object?[] { null };
                yield break;
                
            case IEnumerable<object?[]> objectArrays:
                foreach (var arr in objectArrays)
                    yield return arr;
                yield break;
                
            // Handle primitive types and single values
            case string:
            case bool:
            case byte:
            case sbyte:
            case char:
            case decimal:
            case double:
            case float:
            case int:
            case uint:
            case long:
            case ulong:
            case short:
            case ushort:
                yield return new object?[] { data };
                yield break;
                
            // Handle arrays of primitives
            case int[] intArray:
                foreach (var item in intArray)
                    yield return new object?[] { item };
                yield break;
                
            case string[] stringArray:
                foreach (var item in stringArray)
                    yield return new object?[] { item };
                yield break;
                
            case bool[] boolArray:
                foreach (var item in boolArray)
                    yield return new object?[] { item };
                yield break;
                
            case double[] doubleArray:
                foreach (var item in doubleArray)
                    yield return new object?[] { item };
                yield break;
                
            case float[] floatArray:
                foreach (var item in floatArray)
                    yield return new object?[] { item };
                yield break;
                
            case long[] longArray:
                foreach (var item in longArray)
                    yield return new object?[] { item };
                yield break;
                
            // Handle generic IEnumerable
            case IEnumerable enumerable:
                // Check if this is IEnumerable<object[]> to avoid double wrapping
                var enumerableType = data.GetType();
                if (enumerableType.IsGenericType)
                {
                    var genericArgs = enumerableType.GetGenericArguments();
                    if (genericArgs.Length == 1 && genericArgs[0] == typeof(object[]))
                    {
                        // This is IEnumerable<object[]>, cast and process directly
                        foreach (object[] arr in enumerable)
                            yield return arr;
                        yield break;
                    }
                }
                
                foreach (var item in enumerable)
                {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
                    // Check if item is a tuple using ITuple interface
                    if (item is ITuple tuple)
                    {
                        // Use ITuple interface to access elements
                        var tupleItems = new object?[tuple.Length];
                        for (var i = 0; i < tuple.Length; i++)
                        {
                            tupleItems[i] = tuple[i];
                        }
                        yield return tupleItems;
                    }
                    else 
#endif
                    if (item is object?[] array)
                    {
                        // Already an object array
                        yield return array;
                    }
                    else
                    {
                        // Not a tuple or array, wrap in array
                        yield return new object?[] { item };
                    }
                }
                yield break;
                
            // Single non-enumerable value
            default:
                yield return new object?[] { data };
                yield break;
        }
    }
    
    /// <summary>
    /// Converts IEnumerable<object?[]> to IAsyncEnumerable<object?[]> for async data sources
    /// </summary>
    public static async IAsyncEnumerable<object?[]> ConvertToAsyncEnumerableInternal(
        IEnumerable<object?[]> data,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.Yield(); // Ensure async behavior
        foreach (var item in data)
        {
            ct.ThrowIfCancellationRequested();
            yield return item;
        }
    }
    
    /// <summary>
    /// Converts IAsyncEnumerable<T> to IAsyncEnumerable<object?[]> where T is not object?[]
    /// </summary>
    public static async IAsyncEnumerable<object?[]> ConvertAsyncEnumerableToObjectArrays<T>(
        IAsyncEnumerable<T> source,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in source.WithCancellation(ct))
        {
            yield return new object?[] { item };
        }
    }
    
    /// <summary>
    /// Converts IAsyncEnumerable<(T1, T2)> to IAsyncEnumerable<object?[]>
    /// </summary>
    public static async IAsyncEnumerable<object?[]> ConvertAsyncEnumerableTuple2ToObjectArrays<T1, T2>(
        IAsyncEnumerable<(T1, T2)> source,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var (item1, item2) in source.WithCancellation(ct))
        {
            yield return new object?[] { item1, item2 };
        }
    }
    
    /// <summary>
    /// Converts IAsyncEnumerable<(T1, T2, T3)> to IAsyncEnumerable<object?[]>
    /// </summary>
    public static async IAsyncEnumerable<object?[]> ConvertAsyncEnumerableTuple3ToObjectArrays<T1, T2, T3>(
        IAsyncEnumerable<(T1, T2, T3)> source,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var (item1, item2, item3) in source.WithCancellation(ct))
        {
            yield return new object?[] { item1, item2, item3 };
        }
    }
    
    /// <summary>
    /// Converts IAsyncEnumerable<(T1, T2, T3, T4)> to IAsyncEnumerable<object?[]>
    /// </summary>
    public static async IAsyncEnumerable<object?[]> ConvertAsyncEnumerableTuple4ToObjectArrays<T1, T2, T3, T4>(
        IAsyncEnumerable<(T1, T2, T3, T4)> source,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var (item1, item2, item3, item4) in source.WithCancellation(ct))
        {
            yield return new object?[] { item1, item2, item3, item4 };
        }
    }
    
    /// <summary>
    /// Converts IAsyncEnumerable<(T1, T2, T3, T4, T5)> to IAsyncEnumerable<object?[]>
    /// </summary>
    public static async IAsyncEnumerable<object?[]> ConvertAsyncEnumerableTuple5ToObjectArrays<T1, T2, T3, T4, T5>(
        IAsyncEnumerable<(T1, T2, T3, T4, T5)> source,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var (item1, item2, item3, item4, item5) in source.WithCancellation(ct))
        {
            yield return new object?[] { item1, item2, item3, item4, item5 };
        }
    }
    
    /// <summary>
    /// Wraps a Task<IEnumerable<T>> to ensure it returns object arrays
    /// </summary>
    public static async Task<IEnumerable<object?[]>> WrapTaskEnumerableAsObjectArrays<T>(Task<IEnumerable<T>> task)
    {
        var result = await task;
        return ConvertToObjectArrays(result);
    }
    
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    /// <summary>
    /// Unwraps an ITuple to an object?[] array
    /// </summary>
    public static object?[] UnwrapTuple(ITuple tuple)
    {
        if (tuple == null)
        {
            throw new ArgumentNullException(nameof(tuple));
        }

        var result = new object?[tuple.Length];
        for (var i = 0; i < tuple.Length; i++)
        {
            result[i] = tuple[i];
        }
        return result;
    }
#endif
    
    /// <summary>
    /// Unwraps a ValueTuple<T1, T2> to an object?[] array (optimized for 2-tuples)
    /// </summary>
    public static object?[] UnwrapTuple<T1, T2>((T1, T2) tuple)
    {
        return new object?[] { tuple.Item1, tuple.Item2 };
    }
    
    /// <summary>
    /// Unwraps a ValueTuple<T1, T2, T3> to an object?[] array (optimized for 3-tuples)
    /// </summary>
    public static object?[] UnwrapTuple<T1, T2, T3>((T1, T2, T3) tuple)
    {
        return new object?[] { tuple.Item1, tuple.Item2, tuple.Item3 };
    }
    
    /// <summary>
    /// Unwraps a ValueTuple<T1, T2, T3, T4> to an object?[] array (optimized for 4-tuples)
    /// </summary>
    public static object?[] UnwrapTuple<T1, T2, T3, T4>((T1, T2, T3, T4) tuple)
    {
        return new object?[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4 };
    }
    
    /// <summary>
    /// Unwraps a ValueTuple<T1, T2, T3, T4, T5> to an object?[] array (optimized for 5-tuples)
    /// </summary>
    public static object?[] UnwrapTuple<T1, T2, T3, T4, T5>((T1, T2, T3, T4, T5) tuple)
    {
        return new object?[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5 };
    }
    
    /// <summary>
    /// Unwraps a ValueTuple<T1, T2, T3, T4, T5, T6> to an object?[] array (optimized for 6-tuples)
    /// </summary>
    public static object?[] UnwrapTuple<T1, T2, T3, T4, T5, T6>((T1, T2, T3, T4, T5, T6) tuple)
    {
        return new object?[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6 };
    }
    
    /// <summary>
    /// Unwraps a ValueTuple<T1, T2, T3, T4, T5, T6, T7> to an object?[] array (optimized for 7-tuples)
    /// </summary>
    public static object?[] UnwrapTuple<T1, T2, T3, T4, T5, T6, T7>((T1, T2, T3, T4, T5, T6, T7) tuple)
    {
        return new object?[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7 };
    }
    
    /// <summary>
    /// Unwraps a ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> to an object?[] array (optimized for 8-tuples)
    /// </summary>
    public static object?[] UnwrapTuple<T1, T2, T3, T4, T5, T6, T7, T8>((T1, T2, T3, T4, T5, T6, T7, T8) tuple)
    {
        return new object?[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7, tuple.Item8 };
    }
    
    /// <summary>
    /// Unwraps a ValueTuple with 9 elements to an object?[] array
    /// </summary>
    public static object?[] UnwrapTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>((T1, T2, T3, T4, T5, T6, T7, T8, T9) tuple)
    {
        return new object?[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7, tuple.Item8, tuple.Item9 };
    }
    
    /// <summary>
    /// Unwraps a ValueTuple with 10 elements to an object?[] array
    /// </summary>
    public static object?[] UnwrapTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) tuple)
    {
        return new object?[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7, tuple.Item8, tuple.Item9, tuple.Item10 };
    }
}