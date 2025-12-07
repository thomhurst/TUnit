using TUnit.Core.Logging;

namespace TUnit.Core.Helpers;

internal class Disposer(ILogger logger)
{
    /// <summary>
    /// Disposes an object and propagates any exceptions.
    /// Exceptions are logged but NOT swallowed - callers must handle them.
    /// </summary>
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
            // Log the error for diagnostics
            if (logger != null)
            {
                await logger.LogErrorAsync(e);
            }

            // Propagate the exception - don't silently swallow disposal failures
            // Callers can catch and aggregate if disposing multiple objects
            throw;
        }
    }
}
