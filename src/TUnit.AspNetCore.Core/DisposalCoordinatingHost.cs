using Microsoft.Extensions.Hosting;

namespace TUnit.AspNetCore;

// Minimal-hosting entry points dispose the host from app.Run() while the test factory
// also disposes it. ServiceProvider marks itself disposed before releasing singletons,
// so a second DisposeAsync alone does not wait for those releases to finish.
internal sealed class DisposalCoordinatingHost(IHost inner) : IHost, IAsyncDisposable
{
    private readonly TaskCompletionSource _disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly AsyncLocal<bool> _disposing = new();
    private int _disposalStarted;

    public IServiceProvider Services => inner.Services;

    public Task StartAsync(CancellationToken cancellationToken = default) => inner.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default) => inner.StopAsync(cancellationToken);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposalStarted, 1) != 0)
        {
            return;
        }

        _disposing.Value = true;
        try
        {
            inner.Dispose();
            _disposed.TrySetResult();
        }
        catch (Exception exception)
        {
            _disposed.TrySetException(exception);
            throw;
        }
        finally
        {
            _disposing.Value = false;
        }
    }

    public ValueTask DisposeAsync()
    {
        // The host is itself owned by DI. Disposing its provider re-enters this
        // wrapper on the same execution context; waiting there would deadlock.
        if (_disposing.Value)
        {
            return default;
        }

        return Interlocked.Exchange(ref _disposalStarted, 1) == 0
            ? DisposeInnerAsync()
            : new ValueTask(_disposed.Task);
    }

    private async ValueTask DisposeInnerAsync()
    {
        _disposing.Value = true;
        try
        {
            if (inner is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                inner.Dispose();
            }

            _disposed.TrySetResult();
        }
        catch (Exception exception)
        {
            _disposed.TrySetException(exception);
            throw;
        }
        finally
        {
            _disposing.Value = false;
        }
    }
}

// WebApplicationFactory calls synchronous Dispose after awaiting StopAsync. Finish
// async disposal here, including any disposal already started by app.Run().
internal sealed class DisposalAwaitingHost(IHost inner, DisposalCoordinatingHost applicationHost) : IHost
{
    public IServiceProvider Services => inner.Services;
    public Task StartAsync(CancellationToken cancellationToken = default) => inner.StartAsync(cancellationToken);
    public void Dispose() => inner.Dispose();

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await inner.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await applicationHost.DisposeAsync().ConfigureAwait(false);
        }
    }
}
