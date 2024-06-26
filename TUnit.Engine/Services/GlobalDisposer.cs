namespace TUnit.Engine.Extensions;

internal class GlobalDisposer : IAsyncDisposable
{
    private readonly Disposer _disposer;

    public GlobalDisposer(Disposer disposer)
    {
        _disposer = disposer;
    }
    
    public async ValueTask DisposeAsync()
    {
        foreach (var (_, value) in TestDataContainer.InjectedSharedPerClassType.InnerDictionary)
        {
            await _disposer.DisposeAsync(value);
        }
        
        foreach (var (_, value) in TestDataContainer.InjectedSharedGlobally.InnerDictionary)
        {
            await _disposer.DisposeAsync(value);
        }
        
        foreach (var (_, value) in TestDataContainer.InjectedSharedPerKey.InnerDictionary)
        {
            await _disposer.DisposeAsync(value);
        }
    }
}