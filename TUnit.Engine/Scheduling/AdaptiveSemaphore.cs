using System.Collections.Concurrent;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// A semaphore that supports dynamic adjustment of maximum count
/// </summary>
internal sealed class AdaptiveSemaphore : IDisposable
{
    private readonly object _lock = new();
    private readonly ConcurrentQueue<TaskCompletionSource<bool>> _waiters = new();
    private int _currentCount;
    private int _maxCount;
    private bool _disposed;

    public AdaptiveSemaphore(int initialCount, int maxCount)
    {
        if (initialCount < 0) throw new ArgumentOutOfRangeException(nameof(initialCount));
        if (maxCount < 1) throw new ArgumentOutOfRangeException(nameof(maxCount));
        if (initialCount > maxCount) throw new ArgumentException("Initial count cannot exceed max count");

        _currentCount = initialCount;
        _maxCount = maxCount;
    }

    /// <summary>
    /// Gets the current available count
    /// </summary>
    public int CurrentCount
    {
        get
        {
            lock (_lock)
            {
                return _currentCount;
            }
        }
    }

    /// <summary>
    /// Gets the current maximum count
    /// </summary>
    public int MaxCount
    {
        get
        {
            lock (_lock)
            {
                return _maxCount;
            }
        }
    }

    /// <summary>
    /// Waits to enter the semaphore
    /// </summary>
    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<bool>? waiter = null;

        lock (_lock)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AdaptiveSemaphore));

            if (_currentCount > 0)
            {
                _currentCount--;
                return;
            }

            waiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Enqueue(waiter);
        }

        using (cancellationToken.Register(() => waiter.TrySetCanceled()))
        {
            await waiter.Task.ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Releases one count back to the semaphore
    /// </summary>
    public void Release()
    {
        TaskCompletionSource<bool>? waiterToRelease = null;

        lock (_lock)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AdaptiveSemaphore));

            if (_waiters.TryDequeue(out waiterToRelease))
            {
                // Release directly to a waiter without incrementing count
            }
            else
            {
                // Don't throw if we're at max - this can happen when max count is reduced
                // while tests are still running. Just silently ignore the release.
                if (_currentCount < _maxCount)
                {
                    _currentCount++;
                }
            }
        }

        waiterToRelease?.TrySetResult(true);
    }

    /// <summary>
    /// Adjusts the maximum count of the semaphore
    /// </summary>
    public void AdjustMaxCount(int newMaxCount)
    {
        if (newMaxCount < 1)
            throw new ArgumentOutOfRangeException(nameof(newMaxCount));

        var waitersToRelease = new List<TaskCompletionSource<bool>>();

        lock (_lock)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AdaptiveSemaphore));

            var oldMaxCount = _maxCount;
            _maxCount = newMaxCount;

            // If we're increasing the max count, we might be able to release some waiters
            if (newMaxCount > oldMaxCount)
            {
                var additionalCapacity = newMaxCount - oldMaxCount;
                _currentCount += additionalCapacity;

                // Release waiters if we have capacity
                while (_currentCount > 0 && _waiters.TryDequeue(out var waiter))
                {
                    waitersToRelease.Add(waiter);
                    _currentCount--;
                }
            }
            else if (newMaxCount < oldMaxCount)
            {
                // If decreasing, cap the current count at the new max
                // This prevents issues but allows running tests to complete
                if (_currentCount > newMaxCount)
                {
                    _currentCount = newMaxCount;
                }
            }
        }

        // Release waiters outside the lock to avoid potential deadlocks
        foreach (var waiter in waitersToRelease)
        {
            waiter.TrySetResult(true);
        }
    }

    public void Dispose()
    {
        List<TaskCompletionSource<bool>> waitersToCancel;

        lock (_lock)
        {
            if (_disposed)
                return;

            _disposed = true;
            waitersToCancel = new List<TaskCompletionSource<bool>>();

            while (_waiters.TryDequeue(out var waiter))
            {
                waitersToCancel.Add(waiter);
            }
        }

        foreach (var waiter in waitersToCancel)
        {
            waiter.TrySetCanceled();
        }
    }
}