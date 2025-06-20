using System.Reflection;

namespace TUnit.Core.DataSources;

/// <summary>
/// Provides data from a method that returns test data.
/// </summary>
public class MethodDataSourceProvider : IDataSourceProvider
{
    private readonly MethodInfo _method;
    private readonly object? _instance;
    private readonly bool _isAsync;
    private readonly bool _isShared;
    
    public MethodDataSourceProvider(MethodInfo method, object? instance = null, bool isShared = false)
    {
        _method = method;
        _instance = instance;
        _isShared = isShared;
        
        var returnType = method.ReturnType;
        _isAsync = typeof(IAsyncEnumerable<>).IsAssignableFrom(returnType) ||
                   (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));
    }
    
    public IEnumerable<object?[]> GetData()
    {
        if (_isAsync)
        {
            throw new InvalidOperationException($"Method {_method.Name} returns async data. Use GetDataAsync() instead.");
        }
        
        var result = _method.Invoke(_instance, null);
        
        if (result is IEnumerable<object?[]> arrayEnumerable)
        {
            foreach (var item in arrayEnumerable)
            {
                yield return item;
            }
        }
        else if (result is IEnumerable<object> objectEnumerable)
        {
            foreach (var item in objectEnumerable)
            {
                yield return WrapInArray(item);
            }
        }
        else if (result != null)
        {
            yield return WrapInArray(result);
        }
    }
    
    public async IAsyncEnumerable<object?[]> GetDataAsync()
    {
        if (!_isAsync)
        {
            foreach (var item in GetData())
            {
                yield return item;
            }
            yield break;
        }
        
        var result = _method.Invoke(_instance, null);
        
        if (result is IAsyncEnumerable<object?[]> arrayAsyncEnumerable)
        {
            await foreach (var item in arrayAsyncEnumerable)
            {
                yield return item;
            }
        }
        else if (result is IAsyncEnumerable<object> objectAsyncEnumerable)
        {
            await foreach (var item in objectAsyncEnumerable)
            {
                yield return WrapInArray(item);
            }
        }
    }
    
    public bool IsAsync => _isAsync;
    
    public bool IsShared => _isShared;
    
    private object?[] WrapInArray(object? item)
    {
        // If it's already an array, return as-is
        if (item is object?[] array)
        {
            return array;
        }
        
        // If it's a tuple, unwrap it
        var itemType = item?.GetType();
        if (itemType != null && itemType.IsGenericType && 
            itemType.FullName?.StartsWith("System.ValueTuple`") == true)
        {
            return UnwrapTuple(item);
        }
        
        // Otherwise, wrap in array
        return new[] { item };
    }
    
    private object?[] UnwrapTuple(object tuple)
    {
        var tupleType = tuple.GetType();
        var fields = tupleType.GetFields();
        var values = new List<object?>();
        
        foreach (var field in fields.Where(f => f.Name.StartsWith("Item")))
        {
            var value = field.GetValue(tuple);
            
            // Handle nested tuples for > 7 items
            if (field.Name == "Rest" && value != null && 
                value.GetType().FullName?.StartsWith("System.ValueTuple`") == true)
            {
                values.AddRange(UnwrapTuple(value));
            }
            else
            {
                values.Add(value);
            }
        }
        
        return values.ToArray();
    }
}