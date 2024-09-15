using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public class ResettableLazy<
#if NET8_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] 
#endif
    T> : IAsyncDisposable
{
    private readonly Func<T> _factory;

    private Lazy<T> _lazy;

    public T Value => _lazy.Value;

    public ResettableLazy(Func<T> factory)
    {
        _factory = factory;
        _lazy = new Lazy<T>(factory);
    }

#if NET8_0_OR_GREATER
    public async Task ResetLazy()
    {
        await DisposeAsync();
        
        _lazy = new Lazy<T>(_factory);
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
#endif
}