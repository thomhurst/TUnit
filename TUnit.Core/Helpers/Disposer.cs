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
                await asyncDisposable.DisposeAsync();
            }
            else if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        catch (Exception e)
        {
            if (logger != null)
            {
                await logger.LogErrorAsync(e);
            }
        }
    }
}