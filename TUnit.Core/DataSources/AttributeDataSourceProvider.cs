using System.Diagnostics.CodeAnalysis;

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
        // For now, return empty data until we implement proper reflection
        // The TestBuilder will handle actual data provision through other mechanisms
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
    
    public bool IsShared => false;
}