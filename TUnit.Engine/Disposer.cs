using Microsoft.Testing.Platform.Logging;

namespace TUnit.Engine;

internal class Disposer
{
    private readonly ILogger<Disposer> _logger;

    public Disposer(ILogger<Disposer> logger)
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

            if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        catch (Exception e)
        {
            await _logger.LogErrorAsync(e);
        }
    }
}