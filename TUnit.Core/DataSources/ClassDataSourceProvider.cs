using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.DataSources;

/// <summary>
/// Provides data from a class that has methods or properties returning test data.
/// </summary>
public class ClassDataSourceProvider : IDataSourceProvider
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | 
                                DynamicallyAccessedMemberTypes.PublicMethods | 
                                DynamicallyAccessedMemberTypes.PublicProperties)]
    private readonly Type _dataSourceType;
    private readonly bool _isShared;
    private object? _sharedInstance;
    
    public ClassDataSourceProvider(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | 
                                    DynamicallyAccessedMemberTypes.PublicMethods | 
                                    DynamicallyAccessedMemberTypes.PublicProperties)]
        Type dataSourceType, 
        bool isShared = false)
    {
        _dataSourceType = dataSourceType;
        _isShared = isShared;
    }
    
    public IEnumerable<object?[]> GetData()
    {
        var instance = GetOrCreateInstance();
        
        // Look for a GetData method
        var getDataMethod = _dataSourceType.GetMethod("GetData", BindingFlags.Public | BindingFlags.Instance);
        if (getDataMethod != null)
        {
            var result = getDataMethod.Invoke(instance, null);
            if (result is IEnumerable<object?[]> arrayEnumerable)
            {
                return arrayEnumerable;
            }
            else if (result is IEnumerable<object> objectEnumerable)
            {
                return objectEnumerable.Select(item => new[] { item });
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
    
    public bool IsShared => _isShared;
    
    private object GetOrCreateInstance()
    {
        if (_isShared)
        {
            return _sharedInstance ??= Activator.CreateInstance(_dataSourceType)!;
        }
        
        return Activator.CreateInstance(_dataSourceType)!;
    }
}