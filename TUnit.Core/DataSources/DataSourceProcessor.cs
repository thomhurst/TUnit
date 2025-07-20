using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.DataSources;

/// <summary>
/// Shared processor for data sources that ensures consistent behavior between AOT and reflection modes
/// </summary>
public static class DataSourceProcessor
{
    /// <summary>
    /// AOT-compatible method to process typed generator items
    /// </summary>
    public static async Task<List<object?[]>> ProcessTypedGeneratorItemAsync<T>(Func<Task<T>> taskFunc)
    {
        var items = new List<object?[]>();
        var data = await taskFunc().ConfigureAwait(false);
        
        if (data is object?[] array)
        {
            items.Add(array);
        }
        else if (data != null)
        {
            items.Add([(object?)data]);
        }
        
        return items;
    }

    /// <summary>
    /// AOT-compatible method to process typed array generator items
    /// </summary>
    public static async Task<List<object?[]>> ProcessTypedArrayGeneratorItemAsync<T>(Func<Task<T[]>> taskFunc)
    {
        var items = new List<object?[]>();
        var data = await taskFunc().ConfigureAwait(false);
        
        if (data != null)
        {
            items.Add(data.Cast<object?>().ToArray());
        }
        
        return items;
    }

    /// <summary>
    /// Processes a generator result item and extracts the data values
    /// This method uses reflection and is only suitable for reflection mode
    /// </summary>
    [RequiresUnreferencedCode("This method uses reflection to process data sources")]
    [RequiresDynamicCode("This method may create types at runtime")]
    public static async Task<List<object?[]>> ProcessGeneratorItemAsync(object? item)
    {
        var items = new List<object?[]>();
        
        if (item == null)
        {
            return items;
        }

        // Handle Func<Task<T>> patterns
        if (item is Func<Task<object?[]?>> taskArrayFunc)
        {
            var data = await taskArrayFunc().ConfigureAwait(false);
            if (data != null)
            {
                items.Add(data);
            }
        }
        else if (item is Func<Task<object?>> taskFunc)
        {
            var data = await taskFunc().ConfigureAwait(false);
            items.Add([data]);
        }
        // Handle direct Task<T> patterns
        else if (item is Task<object?[]?> taskArray)
        {
            var data = await taskArray.ConfigureAwait(false);
            if (data != null)
            {
                items.Add(data);
            }
        }
        else if (item is Task<object?> task)
        {
            var data = await task.ConfigureAwait(false);
            items.Add([data]);
        }
        // Handle synchronous Func<T> patterns
        else if (item is Func<object?[]?> func)
        {
            var data = func();
            if (data != null)
            {
                items.Add(data);
            }
        }
        else if (item is Func<object?> singleFunc)
        {
            var data = singleFunc();
            items.Add([data]);
        }
        // Handle direct arrays
        else if (item is object?[] array)
        {
            items.Add(array);
        }
        // Handle single values
        else
        {
            items.Add([item]);
        }
        
        return items;
    }

    /// <summary>
    /// AOT-compatible typed value resolver for known types
    /// </summary>
    public static async Task<object?> ResolveTypedValueAsync<T>(Func<Task<T>> taskFunc)
    {
        return await taskFunc().ConfigureAwait(false);
    }

    /// <summary>
    /// AOT-compatible synchronous typed value resolver
    /// </summary>
    public static object? ResolveTypedValue<T>(Func<T> func)
    {
        return func();
    }

    /// <summary>
    /// Resolves a value that might be wrapped in a Func or Task
    /// This method uses reflection and is only suitable for reflection mode
    /// </summary>
    [RequiresUnreferencedCode("This method uses reflection to resolve values")]
    [RequiresDynamicCode("This method may invoke methods dynamically")]
    public static async Task<object?> ResolveValueAsync(object? value)
    {
        if (value == null)
        {
            return null;
        }

        var type = value.GetType();

        // Check if it's a Task<T>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var task = (Task)value;
            await task.ConfigureAwait(false);
            
            var resultProperty = type.GetProperty("Result");
            return resultProperty?.GetValue(task);
        }

        // Check if it's a ValueTask<T>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            // Convert to Task and await
            var asTaskMethod = type.GetMethod("AsTask");
            if (asTaskMethod != null)
            {
                var task = (Task?)asTaskMethod.Invoke(value, null);
                if (task != null)
                {
                    await task.ConfigureAwait(false);
                    var resultProperty = task.GetType().GetProperty("Result");
                    return resultProperty?.GetValue(task);
                }
            }
        }

        // Check if it's a Func<T>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Func<>))
        {
            var invokeMethod = type.GetMethod("Invoke");
            var result = invokeMethod?.Invoke(value, null);
            
            // Recursively resolve in case Func returns Task
            return await ResolveValueAsync(result).ConfigureAwait(false);
        }

        // Check for other delegate types that might need invocation
        if (typeof(Delegate).IsAssignableFrom(type))
        {
            var invokeMethod = type.GetMethod("Invoke");
            if (invokeMethod != null && invokeMethod.GetParameters().Length == 0)
            {
                var result = invokeMethod.Invoke(value, null);
                
                // Recursively resolve in case delegate returns Task
                return await ResolveValueAsync(result).ConfigureAwait(false);
            }
        }

        return value;
    }

    /// <summary>
    /// Processes method data source results into a consistent format
    /// This method uses reflection for tuple processing and is not AOT-compatible
    /// </summary>
    [RequiresUnreferencedCode("This method uses reflection for tuple processing")]
    public static IEnumerable<object?[]> ProcessMethodDataSourceResult(object? result)
    {
        if (result == null)
        {
            yield break;
        }

        var resultType = result.GetType();

        // Handle IEnumerable<object?[]>
        if (result is IEnumerable<object?[]> objectArrayEnum)
        {
            foreach (var item in objectArrayEnum)
            {
                yield return item ?? [
                ];
            }
            yield break;
        }

        // Handle IEnumerable<object> (but not string)
        if (result is IEnumerable<object> objectEnum && !(result is string))
        {
            foreach (var item in objectEnum)
            {
                yield return [item];
            }
            yield break;
        }

        // Handle arrays of tuples
        if (TryProcessTupleArray(result, resultType))
        {
            foreach (var values in ProcessTupleArray(result, resultType))
            {
                yield return values;
            }
            yield break;
        }

        // Handle single tuple
        if (TryProcessSingleTuple(result, resultType))
        {
            yield return ProcessSingleTuple(result, resultType);
            yield break;
        }

        // Handle direct array
        if (result is object?[] directArray)
        {
            yield return directArray;
            yield break;
        }

        // Handle IEnumerable (generic catch-all)
        if (result is System.Collections.IEnumerable enumerable)
        {
            var items = new List<object?>();
            foreach (var item in enumerable)
            {
                items.Add(item);
            }
            
            if (items.Count > 0)
            {
                yield return items.ToArray();
            }
            yield break;
        }

        // Single value
        yield return [result];
    }

    #region Tuple Processing Helpers (Reflection-based, not AOT-compatible)

    [RequiresUnreferencedCode("Tuple processing requires reflection")]
    private static bool TryProcessTupleArray(object result, Type resultType)
    {
        return resultType.IsArray && resultType.GetElementType()?.Name.StartsWith("ValueTuple") == true;
    }

    [RequiresUnreferencedCode("Tuple processing requires reflection")]
    private static IEnumerable<object?[]> ProcessTupleArray(object result, Type resultType)
    {
        var array = (Array)result;
        foreach (var item in array)
        {
            if (item != null)
            {
                var tupleType = item.GetType();
                var fields = tupleType.GetFields();
                var values = fields.Select(f => f.GetValue(item)).ToArray();
                yield return values;
            }
        }
    }

    [RequiresUnreferencedCode("Tuple processing requires reflection")]
    private static bool TryProcessSingleTuple(object result, Type resultType)
    {
        return resultType.Name.StartsWith("ValueTuple");
    }

    [RequiresUnreferencedCode("Tuple processing requires reflection")]
    private static object?[] ProcessSingleTuple(object result, Type resultType)
    {
        var fields = resultType.GetFields();
        return fields.Select(f => f.GetValue(result)).ToArray();
    }

    #endregion
}