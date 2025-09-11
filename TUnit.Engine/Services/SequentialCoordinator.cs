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
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        
        return new CoordinationToken(semaphore);
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
    /// Token that releases the coordination lock when disposed.
    /// </summary>
    private sealed class CoordinationToken : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        public CoordinationToken(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _semaphore.Release();
                _disposed = true;
            }
        }
    }
}