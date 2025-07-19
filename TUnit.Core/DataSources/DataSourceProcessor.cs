using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.DataSources;

/// <summary>
/// Shared processor for data sources that ensures consistent behavior between AOT and reflection modes
/// </summary>
public static class DataSourceProcessor
{
    /// <summary>
    /// Processes a generator result item and extracts the data values
    /// </summary>
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
            items.Add(new[] { data });
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
            items.Add(new[] { data });
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
            items.Add(new[] { data });
        }
        // Handle direct arrays
        else if (item is object?[] array)
        {
            items.Add(array);
        }
        // Handle single values
        else
        {
            items.Add(new[] { item });
        }
        
        return items;
    }

    /// <summary>
    /// Resolves a value that might be wrapped in a Func or Task
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2075:Target method return value does not satisfy annotation requirements.",
        Justification = "This is shared code used by reflection mode which doesn't support AOT")]
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
    /// </summary>
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
                yield return item ?? Array.Empty<object?>();
            }
            yield break;
        }

        // Handle IEnumerable<object> (but not string)
        if (result is IEnumerable<object> objectEnum && !(result is string))
        {
            foreach (var item in objectEnum)
            {
                yield return new[] { item };
            }
            yield break;
        }

        // Handle arrays of tuples
        if (resultType.IsArray && resultType.GetElementType()?.Name.StartsWith("ValueTuple") == true)
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
            yield break;
        }

        // Handle single tuple
        if (resultType.Name.StartsWith("ValueTuple"))
        {
            var fields = resultType.GetFields();
            var values = fields.Select(f => f.GetValue(result)).ToArray();
            yield return values;
            yield break;
        }

        // Handle direct array
        if (result is object?[] directArray)
        {
            yield return directArray;
            yield break;
        }

        // Handle IEnumerable (generic catch-all)
        if (result is IEnumerable enumerable)
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
        yield return new[] { result };
    }
}