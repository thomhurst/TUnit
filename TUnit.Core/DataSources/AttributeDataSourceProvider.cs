using System.Reflection;

namespace TUnit.Core.DataSources;

/// <summary>
/// Provides data from a custom data attribute that implements IDataAttribute.
/// </summary>
public class AttributeDataSourceProvider : IDataSourceProvider
{
    private readonly object _dataAttribute;
    
    public AttributeDataSourceProvider(object dataAttribute)
    {
        _dataAttribute = dataAttribute;
    }
    
    public IEnumerable<object?[]> GetData()
    {
        // Use reflection to call the Provide method
        var provideMethod = _dataAttribute.GetType().GetMethod("Provide", BindingFlags.Public | BindingFlags.Instance);
        if (provideMethod != null)
        {
            var result = provideMethod.Invoke(_dataAttribute, new object?[] { null });
            if (result is IEnumerable<object?[]> enumerable)
            {
                return enumerable;
            }
        }
        
        return Enumerable.Empty<object?[]>();
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
    
    public bool IsShared 
    {
        get
        {
            var sharedProperty = _dataAttribute.GetType().GetProperty("IsShared", BindingFlags.Public | BindingFlags.Instance);
            if (sharedProperty != null && sharedProperty.PropertyType == typeof(bool))
            {
                return (bool)(sharedProperty.GetValue(_dataAttribute) ?? false);
            }
            return false;
        }
    }
}