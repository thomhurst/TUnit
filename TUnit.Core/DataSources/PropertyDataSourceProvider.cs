using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.DataSources;

/// <summary>
/// Provides data from a property that returns test data.
/// </summary>
public class PropertyDataSourceProvider : IDataSourceProvider
{
    private readonly PropertyInfo _property;
    private readonly object? _instance;
    private readonly bool _isShared;
    
    public PropertyDataSourceProvider(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | 
                                    DynamicallyAccessedMemberTypes.PublicProperties | 
                                    DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type type, 
        string propertyName, 
        bool isShared = false)
    {
        _property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new ArgumentException($"Property {propertyName} not found on type {type}", nameof(propertyName));
        
        _isShared = isShared;
        
        // For static properties, we don't need an instance
        if (!_property.GetMethod!.IsStatic)
        {
            _instance = Activator.CreateInstance(type);
        }
    }
    
    public IEnumerable<object?[]> GetData()
    {
        var result = _property.GetValue(_instance);
        
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
        foreach (var item in GetData())
        {
            yield return item;
        }
        await Task.CompletedTask;
    }
    
    public bool IsAsync => false;
    
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
            itemType.FullName?.StartsWith("System.ValueTuple`") == true && item != null)
        {
            return UnwrapTuple(item);
        }
        
        // Otherwise, wrap in array
        return new[] { item };
    }
    
    private object?[] UnwrapTuple(object tuple)
    {
        var tupleType = tuple.GetType();
        
        #pragma warning disable IL2075 // We know tuples have public fields
        var fields = tupleType.GetFields();
        #pragma warning restore IL2075
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