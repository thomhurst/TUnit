using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public class ResettableLazy<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(Func<T> factory)
    : IAsyncDisposable
{
    private Lazy<T> _lazy = new Lazy<T>(factory);

    public T Value => _lazy.Value;

    public async Task ResetLazy()
    {
        await DisposeAsync();
        
        _lazy = new Lazy<T>(factory);
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_lazy.IsValueCreated)
        {
            if(_lazy.Value is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
        } else if (_lazy.Value is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}