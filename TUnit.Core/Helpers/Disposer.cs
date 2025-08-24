using TUnit.Core.Logging;

namespace TUnit.Core.Helpers;

internal class Disposer(ILogger logger)
{
    public async ValueTask DisposeAsync(object? obj)
    {
        try
        {
            Console.WriteLine($"[Disposer] Disposing {obj?.GetType().Name} (hash: {obj?.GetHashCode()})");
            
            if (obj is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                Console.WriteLine($"[Disposer] Async disposed {obj.GetType().Name} (hash: {obj.GetHashCode()})");
            }
            else if (obj is IDisposable disposable)
            {
                disposable.Dispose();
                Console.WriteLine($"[Disposer] Sync disposed {obj.GetType().Name} (hash: {obj.GetHashCode()})");
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
