using TUnit.Core.Logging;

namespace TUnit.Core.Helpers;

internal class Disposer(ILogger logger)
{
    public async ValueTask DisposeAsync(object? obj)
    {
        try
        {
            
            if (obj is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"[Disposer] Failed to dispose {obj?.GetType().Name}: {e.Message}");
            if (logger != null)
            {
                await logger.LogErrorAsync(e);
            }
        }
    }
}
