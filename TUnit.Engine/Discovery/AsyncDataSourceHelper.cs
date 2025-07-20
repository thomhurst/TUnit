using System.Diagnostics.CodeAnalysis;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Helper for handling async data sources during discovery in reflection mode
/// </summary>
internal static class AsyncDataSourceHelper
{
    /// <summary>
    /// Processes async generator items without evaluating them during discovery
    /// </summary>
    public static List<object?[]> ProcessAsyncGeneratorItemsForDiscovery(object? item)
    {
        var items = new List<object?[]>();
        
        if (item is Func<Task<object?[]?>> taskArrayFunc)
        {
            // For array results, we create a placeholder that preserves the factory
            var placeholder = new AsyncDataSourcePlaceholder 
            { 
                ArrayFactory = taskArrayFunc,
                ExpectedType = GetTaskResultType(taskArrayFunc.GetType())
            };
            items.Add(placeholder.GetPlaceholderArguments());
        }
        else if (item is Func<Task<object?>> singleTaskFunc)
        {
            // For single value results, create appropriate placeholder
            var placeholder = new AsyncDataSourcePlaceholder 
            { 
                SingleValueFactory = singleTaskFunc,
                ExpectedType = GetTaskResultType(singleTaskFunc.GetType())
            };
            items.Add(placeholder.GetPlaceholderArguments());
        }
        else if (IsGenericFuncTask(item, out var resultType))
        {
            // Handle Func<Task<T>> for specific types
            var placeholder = CreateTypedPlaceholder(item!, resultType);
            items.Add(placeholder.GetPlaceholderArguments());
        }
        else
        {
            // Fall back to default processing for non-async items
            return ProcessSyncGeneratorItem(item);
        }
        
        return items;
    }
    
    private static bool IsGenericFuncTask(object? item, out Type? resultType)
    {
        resultType = null;
        if (item == null)
        {
            return false;
        }

        var itemType = item.GetType();
        if (!itemType.IsGenericType)
        {
            return false;
        }

        var genericDef = itemType.GetGenericTypeDefinition();
        if (genericDef != typeof(Func<>))
        {
            return false;
        }

        var returnType = itemType.GetGenericArguments()[0];
        if (!returnType.IsGenericType)
        {
            return false;
        }

        var returnGenericDef = returnType.GetGenericTypeDefinition();
        if (returnGenericDef != typeof(Task<>))
        {
            return false;
        }

        resultType = returnType.GetGenericArguments()[0];
        return true;
    }
    
    private static Type? GetTaskResultType(Type funcType)
    {
        if (!funcType.IsGenericType)
        {
            return null;
        }

        var genericArgs = funcType.GetGenericArguments();
        if (genericArgs.Length == 0)
        {
            return null;
        }

        var returnType = genericArgs[0];
        if (!returnType.IsGenericType)
        {
            return returnType;
        }

        if (returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return returnType.GetGenericArguments()[0];
        }
        
        return returnType;
    }
    
    [UnconditionalSuppressMessage("Trimming", "IL2075:Target method return value does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    private static AsyncDataSourcePlaceholder CreateTypedPlaceholder(object item, Type? resultType)
    {
        // Create a wrapper that preserves the typed factory
        return new AsyncDataSourcePlaceholder
        {
            SingleValueFactory = async () =>
            {
                var funcDelegate = (Delegate)item;
                var taskResult = funcDelegate.DynamicInvoke();
                if (taskResult is Task task)
                {
                    await task.ConfigureAwait(false);
                    var resultProperty = task.GetType().GetProperty("Result");
                    return resultProperty?.GetValue(task);
                }
                return null;
            },
            ExpectedType = resultType
        };
    }
    
    private static List<object?[]> ProcessSyncGeneratorItem(object? item)
    {
        var items = new List<object?[]>();
        
        if (item is Func<object?[]?> func)
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
        else if (item is object?[] array)
        {
            items.Add(array);
        }
        else if (item != null)
        {
            // Try tuple parsing
            if (global::TUnit.Engine.Helpers.TupleHelper.TryParseTupleToObjectArray(item, out var tupleValues))
            {
                items.Add(tupleValues!);
            }
            else
            {
                // Single value - wrap in array
                items.Add([item]);
            }
        }
        
        return items;
    }
    
    /// <summary>
    /// Checks if test data contains async placeholders that need resolution
    /// </summary>
    public static bool ContainsAsyncPlaceholders(object?[] arguments)
    {
        return arguments.Any(arg => arg is AsyncDataSourcePlaceholder || 
                                   (arg is string s && s.StartsWith("<async:")));
    }
    
    /// <summary>
    /// Resolves async placeholders in test arguments
    /// </summary>
    public static async Task<object?[]> ResolveAsyncPlaceholders(object?[] arguments)
    {
        var resolved = new object?[arguments.Length];
        
        for (int i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] is AsyncDataSourcePlaceholder placeholder)
            {
                var data = await placeholder.ResolveAsync().ConfigureAwait(false);
                resolved[i] = data.Length > 0 ? data[0] : null;
            }
            else
            {
                resolved[i] = arguments[i];
            }
        }
        
        return resolved;
    }
}