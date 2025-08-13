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
    private static readonly ConcurrentDictionary<TestContextEvents, HashSet<object>> _contextTrackedObjects = new();
    private static readonly ConcurrentDictionary<(TestContextEvents context, object obj), bool> _incrementTracker = new();

    /// <summary>
    /// Tracks multiple objects for a test context and registers a single disposal handler.
    /// Each object's reference count is incremented once.
    /// </summary>
    /// <param name="events">Events for the test instance</param>
    /// <param name="objects">The objects to track (constructor args, method args, injected properties)</param>
    public static void TrackObjectsForContext(TestContextEvents events, IEnumerable<object?> objects)
    {
        // Simply delegate to TrackObject for each object
        // The safety mechanism in TrackObject will prevent duplicates
        foreach (var obj in objects)
        {
            TrackObject(events, obj);
        }
    }

    /// <summary>
    /// Tracks a single object for a test context. 
    /// For backward compatibility - adds to existing tracked objects for the context.
    /// </summary>
    /// <param name="events">Events for the test instance</param>
    /// <param name="obj">The object to track</param>
    public static void TrackObject(TestContextEvents events, object? obj)
    {
        if (obj == null || ShouldSkipTracking(obj))
        {
            return;
        }

        // Safety check: Only increment once per context-object pair
        // This prevents double-tracking within the same context
        var incrementKey = (events, obj);
        if (!_incrementTracker.TryAdd(incrementKey, true))
        {
            // Already incremented for this context-object pair
            return;
        }

        // Increment the reference count
        var counter = _trackedObjects.GetOrAdd(obj, _ => new Counter());
        counter.Increment();

        // Add to the context's tracked objects or create new tracking
        // Use a factory delegate to ensure disposal handler is registered only once per context
        var contextObjects = _contextTrackedObjects.GetOrAdd(events, e =>
        {
            // Register disposal handler only once when creating the HashSet
            e.OnDispose = e.OnDispose + new Func<object, TestContext, ValueTask>(async (sender, testContext) =>
            {
                if (_contextTrackedObjects.TryRemove(e, out var trackedObjects))
                {
                    foreach (var trackedObj in trackedObjects)
                    {
                        await DecrementAndDisposeIfNeededAsync(trackedObj).ConfigureAwait(false);
                        // Clean up the increment tracker
                        _incrementTracker.TryRemove((e, trackedObj), out _);
                    }
                }
            });
            return new HashSet<object>();
        });

        lock (contextObjects)
        {
            contextObjects.Add(obj);
        }
    }

    /// <summary>
    /// Decrements the reference count for an object and disposes it if count reaches zero.
    /// Pure reference counting: disposal happens immediately when count becomes zero.
    /// </summary>
    /// <param name="obj">The object to decrement and potentially dispose</param>
    private static async ValueTask DecrementAndDisposeIfNeededAsync(object? obj)
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
            
            // Dispose synchronously to avoid race conditions with test class disposal
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
            catch
            {
                // Swallow disposal exceptions to prevent test failures
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
    /// Checks if an object is already being tracked (has a reference count > 0).
    /// This is used to identify shared objects that shouldn't be re-tracked in test contexts.
    /// </summary>
    /// <param name="obj">The object to check</param>
    /// <returns>True if the object is already tracked, false otherwise</returns>
    public static bool IsAlreadyTracked(object? obj)
    {
        if (obj == null || ShouldSkipTracking(obj))
        {
            return false;
        }
        
        return _trackedObjects.TryGetValue(obj, out var counter) && counter.CurrentCount > 0;
    }

    /// <summary>
    /// Clears all tracked references. Use with caution!
    /// </summary>
    public static void Clear()
    {
        _trackedObjects.Clear();
        _contextTrackedObjects.Clear();
        _incrementTracker.Clear();
    }

    /// <summary>
    /// Determines if an object should be skipped from tracking.
    /// </summary>
    private static bool ShouldSkipTracking(object? obj)
    {
        return obj is not IDisposable and not IAsyncDisposable;
    }
}
