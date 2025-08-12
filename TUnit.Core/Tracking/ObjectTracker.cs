using System.Collections.Concurrent;
using TUnit.Core.Helpers;

namespace TUnit.Core.Tracking;

/// <summary>
/// Unified static object tracker that combines reference counting with lifecycle management.
/// Consolidates the functionality of both DataSourceReferenceTracker and ActiveObjectTracker.
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

        // Only register the disposal handler once per object per test context
        var handlerKey = (obj, events);
        if (_registeredHandlers.TryAdd(handlerKey, true))
        {
            events.OnDispose += async (_, _) =>
            {
                await ReleaseObject(obj);
                // Clean up the handler registration tracking
                _registeredHandlers.TryRemove(handlerKey, out _);
            };
        }
    }

    /// <summary>
    /// Decrements the reference count for an object.
    /// Disposes objects when count reaches zero.
    /// </summary>
    /// <param name="obj">The object to release</param>
    /// <returns>Task representing the disposal operation</returns>
    private static async Task ReleaseObject(object? obj)
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

        // Dispose when reference count reaches zero
        if (count <= 0)
        {
            _trackedObjects.TryRemove(obj, out _);

            // Dispose object with timeout to prevent hanging
            try
            {
                var disposeTask = DisposeObjectAsync(obj);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                var completedTask = await Task.WhenAny(disposeTask, timeoutTask).ConfigureAwait(false);
                
                if (completedTask == timeoutTask)
                {
                    // Timeout occurred, but don't throw to prevent hanging the test
                    // The object will be GC'd eventually
                }
                else
                {
                    // Ensure any exceptions from the dispose task are observed
                    await disposeTask.ConfigureAwait(false);
                }
            }
            catch
            {
                // Swallow disposal exceptions to prevent hanging
                // The object will be GC'd eventually if disposal fails
            }
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
    /// Determines if an object should be skipped from tracking.
    /// </summary>
    private static bool ShouldSkipTracking(object? obj)
    {
        return obj is not IDisposable and not IAsyncDisposable;
    }

    /// <summary>
    /// Disposes an object directly without reference counting.
    /// This method should only be used by lifecycle managers (TestDataContainer, etc.)
    /// to dispose shared objects at the appropriate scope boundaries.
    /// </summary>
    /// <param name="obj">The object to dispose</param>
    /// <returns>Task representing the disposal operation</returns>
    public static async Task DisposeTrackedObjectAsync(object? obj)
    {
        if (obj == null || ShouldSkipTracking(obj))
        {
            return;
        }

        try
        {
            await DisposeObjectAsync(obj).ConfigureAwait(false);
        }
        catch
        {
            // Swallow disposal exceptions to prevent lifecycle failures
        }
    }

}
