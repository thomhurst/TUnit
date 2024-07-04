using Microsoft.Testing.Platform.Logging;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Helpers;

internal class Disposer
{
    private readonly TUnitLogger? _logger;

    public Disposer(TUnitLogger? logger)
    {
        _logger = logger;
    }
    
    public async ValueTask DisposeAsync(object? obj)
    {
        try
        {
            if (obj is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        catch (Exception e)
        {
            if (_logger != null)
            {
                await _logger.LogErrorAsync(e);
            }
        }
    }
}