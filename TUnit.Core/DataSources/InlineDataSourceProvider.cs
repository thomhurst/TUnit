namespace TUnit.Core.DataSources;

/// <summary>
/// Provides data from inline values (e.g., [Arguments(1, 2, 3)]).
/// </summary>
public class InlineDataSourceProvider : IDataSourceProvider
{
    private readonly object?[] _data;
    
    public InlineDataSourceProvider(params object?[] data)
    {
        _data = data;
    }
    
    public IEnumerable<object?[]> GetData()
    {
        yield return _data;
    }
    
    public IAsyncEnumerable<object?[]> GetDataAsync()
    {
        return GetData().ToAsyncEnumerable();
    }
    
    public bool IsAsync => false;
    
    public bool IsShared => false;
}

internal static class EnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
        }
        await Task.CompletedTask; // Suppress warning
    }
}