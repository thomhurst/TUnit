using System.Collections.Concurrent;
using TUnit.Core.Helpers;

namespace TUnit.Core.Tracking;

/// <summary>
/// Pure reference counting object tracker for disposable objects.
/// Objects are disposed when their reference count reaches zero, regardless of sharing type.
/// </summary>
internal static class ObjectTracker
{
    private static readonly ConcurrentDictionary<object, Counter> _trackedObjects = new();
    private static readonly ConcurrentDictionary<(object obj, TestContextEvents events), bool> _registeredHandlers = new();

    /// <summary>
    /// Tracks an object and increments its reference count.
    /// </summary>
    /// <param name="events">Events for the test instance</param>
    /// <param name="obj">The object to track</param>
    /// <returns>The tracked object (same instance)</returns>
    public static void TrackObject(TestContextEvents events, object? obj)
    {
        if (obj == null || ShouldSkipTracking(obj))
        {
            return;
        }

        var counter = _trackedObjects.GetOrAdd(obj, _ => new Counter());
        counter.Increment();

        // Only register the decrement handler once per object per test context
        var handlerKey = (obj, events);
        if (_registeredHandlers.TryAdd(handlerKey, true))
        {
            events.OnDispose = events.OnDispose + new Func<object, TestContext, ValueTask>((sender, testContext) =>
            {
                DecrementAndDisposeIfNeeded(obj);
                // Clean up the handler registration tracking
                _registeredHandlers.TryRemove(handlerKey, out _);
                return default(ValueTask);
            });
        }
    }

    /// <summary>
    /// Decrements the reference count for an object and disposes it if count reaches zero.
    /// Pure reference counting: disposal happens immediately when count becomes zero.
    /// </summary>
    /// <param name="obj">The object to decrement and potentially dispose</param>
    private static void DecrementAndDisposeIfNeeded(object? obj)
    {
        if (obj == null)
        {
            return;
        }

        if (!_trackedObjects.TryGetValue(obj, out var counter))
        {
            return;
        }

        var count = counter.Decrement();

        // Dispose ANY object when reference count reaches zero - pure reference counting
        if (count <= 0)
        {
            _trackedObjects.TryRemove(obj, out _);
            
            // Schedule disposal on a background task to avoid blocking the test disposal
            _ = Task.Run(async () =>
            {
                try
                {
                    await DisposeObjectAsync(obj);
                }
                catch
                {
                    // Swallow disposal exceptions to prevent test failures
                    // The object will be GC'd eventually if disposal fails
                }
            });
        }
    }

    /// <summary>
    /// Disposes an object using the appropriate disposal method.
    /// </summary>
    /// <param name="obj">The object to dispose</param>
    private static async Task DisposeObjectAsync(object obj)
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

    /// <summary>
    /// Gets the reference counter for a tracked object.
    /// </summary>
    /// <param name="obj">The object to get counter for</param>
    /// <returns>Counter or null if not tracked</returns>
    public static Counter? GetReferenceInfo(object? obj)
    {
        return obj != null && _trackedObjects.TryGetValue(obj, out var counter) ? counter : null;
    }

    /// <summary>
    /// Tries to get the reference counter for an object.
    /// </summary>
    /// <param name="obj">The object to check</param>
    /// <param name="counter">The reference counter if found</param>
    /// <returns>True if the object is tracked</returns>
    public static bool TryGetReference(object? obj, out Counter? counter)
    {
        counter = null;
        if (obj == null || ShouldSkipTracking(obj))
        {
            return false;
        }

        return _trackedObjects.TryGetValue(obj, out counter);
    }

    /// <summary>
    /// Removes an object from tracking without decrementing its count.
    /// </summary>
    /// <param name="obj">The object to remove</param>
    /// <returns>True if the object was removed</returns>
    public static bool RemoveObject(object? obj)
    {
        if (obj == null)
        {
            return false;
        }

        return _trackedObjects.TryRemove(obj, out _);
    }

    /// <summary>
    /// Gets the count of currently tracked objects.
    /// </summary>
    public static int TrackedObjectCount => _trackedObjects.Count;

    /// <summary>
    /// Clears all tracked references. Use with caution!
    /// </summary>
    public static void Clear()
    {
        _trackedObjects.Clear();
    }

    /// <summary>
    /// Determines if an object should be skipped from tracking.
    /// </summary>
    private static bool ShouldSkipTracking(object? obj)
    {
        return obj is not IDisposable and not IAsyncDisposable;
    }
}
