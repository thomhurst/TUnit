using System.Collections.Concurrent;
using TUnit.Core.Models;

namespace TUnit.Engine.Services;

/// <summary>
/// Manages sequential coordination for test execution contexts.
/// Ensures only one class processes at a time within each execution group.
/// </summary>
internal sealed class SequentialCoordinator : IDisposable
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _groupSemaphores = new();
    private bool _disposed;

    /// <summary>
    /// Acquires exclusive access for the specified execution group.
    /// Returns a disposable token that releases the lock when disposed.
    /// </summary>
    public async Task<IDisposable> AcquireAsync(string groupKey, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        
        var semaphore = _groupSemaphores.GetOrAdd(groupKey, _ => new SemaphoreSlim(1, 1));
        
        try
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // If the semaphore was disposed while we were waiting, try to get a new one
            ThrowIfDisposed();
            semaphore = _groupSemaphores.GetOrAdd(groupKey, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        
        return new CoordinationToken(semaphore, groupKey, this);
    }
    
    /// <summary>
    /// Acquires exclusive access for the specified execution group with a timeout.
    /// Returns a disposable token that releases the lock when disposed.
    /// </summary>
    public async Task<IDisposable> AcquireAsync(string groupKey, CancellationToken cancellationToken, TimeSpan timeout)
    {
        ThrowIfDisposed();
        
        var semaphore = _groupSemaphores.GetOrAdd(groupKey, _ => new SemaphoreSlim(1, 1));
        
        try
        {
            var acquired = await semaphore.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
            if (!acquired)
            {
                throw new TimeoutException($"Failed to acquire coordination lock for group '{groupKey}' within {timeout.TotalMilliseconds}ms. Possible deadlock detected.");
            }
        }
        catch (ObjectDisposedException)
        {
            // If the semaphore was disposed while we were waiting, try to get a new one
            ThrowIfDisposed();
            semaphore = _groupSemaphores.GetOrAdd(groupKey, _ => new SemaphoreSlim(1, 1));
            var acquired = await semaphore.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
            if (!acquired)
            {
                throw new TimeoutException($"Failed to acquire coordination lock for group '{groupKey}' within {timeout.TotalMilliseconds}ms after semaphore recovery. Possible deadlock detected.");
            }
        }
        
        return new CoordinationToken(semaphore, groupKey, this);
    }

    /// <summary>
    /// Gets the coordination key for an execution context.
    /// </summary>
    public static string GetCoordinationKey(TestExecutionContext executionContext)
    {
        return executionContext.ContextType switch
        {
            ExecutionContextType.NotInParallel => "NotInParallel",
            ExecutionContextType.KeyedNotInParallel => $"KeyedNotInParallel:{executionContext.GroupKey}",
            ExecutionContextType.ParallelGroup => $"ParallelGroup:{executionContext.GroupKey}:{executionContext.Order}",
            _ => "Parallel"
        };
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var semaphore in _groupSemaphores.Values)
        {
            semaphore.Dispose();
        }
        
        _groupSemaphores.Clear();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SequentialCoordinator));
        }
    }

    /// <summary>
    /// Attempts to clean up unused semaphores to prevent memory leaks.
    /// This is best-effort cleanup and doesn't guarantee immediate removal.
    /// </summary>
    internal void TryCleanupSemaphore(string groupKey)
    {
        if (_disposed) return;
        
        // Try to remove semaphores that are no longer being waited on
        if (_groupSemaphores.TryGetValue(groupKey, out var semaphore))
        {
            // Only remove if no one is waiting and it's available
            if (semaphore.CurrentCount > 0)
            {
                // Try to remove, but don't force it if someone else is using it
                _groupSemaphores.TryRemove(groupKey, out _);
                semaphore.Dispose();
            }
        }
    }

    /// <summary>
    /// Token that releases the coordination lock when disposed.
    /// </summary>
    private sealed class CoordinationToken : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly string _groupKey;
        private readonly SequentialCoordinator _coordinator;
        private bool _disposed;

        public CoordinationToken(SemaphoreSlim semaphore, string groupKey, SequentialCoordinator coordinator)
        {
            _semaphore = semaphore;
            _groupKey = groupKey;
            _coordinator = coordinator;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    _semaphore.Release();
                }
                catch (ObjectDisposedException)
                {
                    // Semaphore was already disposed, which is fine
                }
                finally
                {
                    _disposed = true;
                    // Try to clean up the semaphore if it's no longer needed
                    _coordinator.TryCleanupSemaphore(_groupKey);
                }
            }
        }
    }
}