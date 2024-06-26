using Microsoft.Testing.Platform.Logging;

namespace TUnit.Engine.Helpers;

internal class Disposer
{
    private readonly ILogger<Disposer> _logger;

    public Disposer(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Disposer>();
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