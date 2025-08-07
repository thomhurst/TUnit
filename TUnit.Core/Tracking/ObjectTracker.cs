using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core.Helpers;

namespace TUnit.Core.Tracking;

/// <summary>
/// Unified static object tracker that combines reference counting with lifecycle management.
/// Consolidates the functionality of both DataSourceReferenceTracker and ActiveObjectTracker.
/// </summary>
internal static class ObjectTracker
{
    private static readonly ConcurrentDictionary<object, Counter> _trackedObjects = new();
    private static readonly ConcurrentDictionary<object, (SharedType Scope, object? ScopeKey)> _objectScopes = new();

    /// <summary>
    /// Tracks an object and increments its reference count.
    /// </summary>
    /// <param name="events">Events for the test instance</param>
    /// <param name="obj">The object to track</param>
    /// <param name="scope">The scope of the object</param>
    /// <param name="scopeKey">The scope key (e.g., Type for PerClass, Assembly for PerAssembly)</param>
    /// <returns>The tracked object (same instance)</returns>
    public static void TrackObject(TestContextEvents events, object? obj, SharedType scope = SharedType.None, object? scopeKey = null)
    {
        if (obj == null || ShouldSkipTracking(obj))
        {
            return;
        }

        var counter = _trackedObjects.GetOrAdd(obj, _ => new Counter());
        _objectScopes.TryAdd(obj, (scope, scopeKey));

        counter.Increment();

        // Only register disposal for test-scoped objects on individual test events
        // Class and Assembly scoped objects will be disposed by scope-specific disposal
        if (scope == SharedType.None)
        {
            events.OnDispose += async (_, _) =>
            {
                await ReleaseObject(obj);
            };
        }
    }

    /// <summary>
    /// Disposes objects for a specific scope and scope key
    /// </summary>
    /// <param name="scope">The scope to dispose objects for</param>
    /// <param name="scopeKey">The scope key to match</param>
    public static async Task DisposeObjectsForScope(SharedType scope, object? scopeKey = null)
    {
        var objectsToDispose = new List<object>();
        
        foreach (var kvp in _objectScopes)
        {
            var (objectScope, objectScopeKey) = kvp.Value;
            if (objectScope == scope && Equals(objectScopeKey, scopeKey))
            {
                objectsToDispose.Add(kvp.Key);
            }
        }

        foreach (var obj in objectsToDispose)
        {
            await ReleaseObject(obj, forceDispose: true);
        }
    }

    /// <summary>
    /// Decrements the reference count for an object and optionally disposes it.
    /// </summary>
    /// <param name="obj">The object to release</param>
    /// <param name="forceDispose">If true, dispose regardless of reference count</param>
    /// <returns>True if the object has no more references and was removed from tracking</returns>
    private static async Task ReleaseObject(object? obj, bool forceDispose = false)
    {
        if (obj == null)
        {
            return;
        }

        if (!_trackedObjects.TryGetValue(obj, out var counter))
        {
            return;
        }

        var count = forceDispose ? 0 : counter.Decrement();

        if (count <= 0)
        {
            _trackedObjects.TryRemove(obj, out _);
            _objectScopes.TryRemove(obj, out _);

            if (obj is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (obj is IDisposable disposable)
            {
                disposable.Dispose();
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

        _objectScopes.TryRemove(obj, out _);
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
        _objectScopes.Clear();
    }

    /// <summary>
    /// Determines if an object should be skipped from tracking.
    /// </summary>
    private static bool ShouldSkipTracking(object? obj)
    {
        return obj is not IDisposable and not IAsyncDisposable;
    }
}
