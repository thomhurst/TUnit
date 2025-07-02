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
    [RequiresUnreferencedCode("This method uses reflection to handle tuple types and may not work correctly with trimming.")]
    [RequiresDynamicCode("This method uses reflection to handle tuple types and may require JIT compilation.")]
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
                    // Check if item is a tuple type
                    var itemType = item?.GetType();
                    var typeName = itemType?.FullName;
                    
                    if (typeName != null && typeName.StartsWith("System.ValueTuple`"))
                    {
                        // Handle tuple types using reflection
                        var fields = itemType!.GetFields();
                        var tupleItems = new object?[fields.Length];
                        for (int i = 0; i < fields.Length; i++)
                        {
                            tupleItems[i] = fields[i].GetValue(item);
                        }
                        yield return tupleItems;
                    }
                    else
                    {
                        // Not a tuple, wrap in array
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
}