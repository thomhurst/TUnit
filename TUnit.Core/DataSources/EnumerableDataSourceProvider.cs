namespace TUnit.Core.DataSources;

/// <summary>
/// Provides data from an enumerable collection.
/// </summary>
public class EnumerableDataSourceProvider : IDataSourceProvider
{
    private readonly IEnumerable<object?[]> _data;
    private readonly bool _isShared;
    
    public EnumerableDataSourceProvider(IEnumerable<object?[]> data, bool isShared = false)
    {
        _data = data;
        _isShared = isShared;
    }
    
    public IEnumerable<object?[]> GetData()
    {
        return _data;
    }
    
    public IAsyncEnumerable<object?[]> GetDataAsync()
    {
        return _data.ToAsyncEnumerable();
    }
    
    public bool IsAsync => false;
    
    public bool IsShared => _isShared;
}