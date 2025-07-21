using System.Collections.Concurrent;
using TUnit.Core.Helpers;

namespace TUnit.Core.Tracking;

/// <summary>
/// Unified object tracker that combines reference counting with optional lifecycle management.
/// Consolidates the functionality of both DataSourceReferenceTracker and ActiveObjectTracker.
/// </summary>
public class UnifiedObjectTracker
{
    private readonly ConcurrentDictionary<object, Counter> _trackedObjects = new();
    private readonly bool _enableRecursiveTracking;

    /// <summary>
    /// Creates a new UnifiedObjectTracker with specified options.
    /// </summary>
    /// <param name="enableRecursiveTracking">If true, collections will be recursively tracked</param>
    public UnifiedObjectTracker(bool enableRecursiveTracking = false)
    {
        _enableRecursiveTracking = enableRecursiveTracking;
    }


    /// <summary>
    /// Tracks an object and increments its reference count.
    /// </summary>
    /// <param name="obj">The object to track</param>
    /// <returns>The tracked object (same instance)</returns>
    public T TrackObject<T>(T obj) where T : notnull
    {
        if (ShouldSkipTracking(obj))
        {
            return obj;
        }

        var counter = _trackedObjects.GetOrAdd(obj, 
            _ => new Counter());
        
        counter.Increment();

        // Handle recursive tracking for collections if enabled
        if (_enableRecursiveTracking && obj is System.Collections.IEnumerable enumerable && !(obj is string))
        {
            foreach (var item in enumerable)
            {
                if (item != null)
                {
                    TrackObject(item);
                }
            }
        }
        
        return obj;
    }

    /// <summary>
    /// Tracks an object if it's not null.
    /// </summary>
    /// <param name="obj">The object to track</param>
    /// <returns>The same object instance or null</returns>
    public object? TrackObject(object? obj)
    {
        if (obj == null)
        {
            return null;
        }

        if (ShouldSkipTracking(obj))
        {
            return obj;
        }

        var counter = _trackedObjects.GetOrAdd(obj, 
            _ => new Counter());
        
        counter.Increment();

        // Handle recursive tracking for collections if enabled
        if (_enableRecursiveTracking && obj is System.Collections.IEnumerable enumerable && !(obj is string))
        {
            foreach (var item in enumerable)
            {
                TrackObject(item);
            }
        }

        return obj;
    }

    /// <summary>
    /// Tracks multiple objects.
    /// </summary>
    /// <param name="objects">The objects to track</param>
    public void TrackObjects(IEnumerable<object?> objects)
    {
        foreach (var obj in objects)
        {
            TrackObject(obj);
        }
    }

    /// <summary>
    /// Decrements the reference count for an object and optionally disposes it.
    /// </summary>
    /// <param name="obj">The object to release</param>
    /// <returns>True if the object has no more references and was removed from tracking</returns>
    public async Task<bool> ReleaseObject(object? obj)
    {
        if (obj == null || ShouldSkipTracking(obj))
        {
            return false;
        }

        if (!_trackedObjects.TryGetValue(obj, out var counter))
        {
            return false;
        }

        var count = counter.Decrement();
        
        if (count <= 0)
        {
            _trackedObjects.TryRemove(obj, out _);
            
            // Handle recursive release for collections if enabled
            if (_enableRecursiveTracking && obj is System.Collections.IEnumerable enumerable && !(obj is string))
            {
                foreach (var item in enumerable)
                {
                    if (item != null)
                    {
                        await ReleaseObject(item);
                    }
                }
            }

            // Always auto-dispose when reference count reaches zero
            if (obj is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Gets the reference counter for a tracked object.
    /// </summary>
    /// <param name="obj">The object to get counter for</param>
    /// <returns>Counter or null if not tracked</returns>
    public Counter? GetReferenceInfo(object? obj)
    {
        return obj != null && _trackedObjects.TryGetValue(obj, out var counter) ? counter : null;
    }

    /// <summary>
    /// Tries to get the reference counter for an object.
    /// </summary>
    /// <param name="obj">The object to check</param>
    /// <param name="counter">The reference counter if found</param>
    /// <returns>True if the object is tracked</returns>
    public bool TryGetReference(object? obj, out Counter? counter)
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
    public bool RemoveObject(object? obj)
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
    public int TrackedObjectCount => _trackedObjects.Count;

    /// <summary>
    /// Clears all tracked references. Use with caution!
    /// </summary>
    public void Clear()
    {
        _trackedObjects.Clear();
    }

    /// <summary>
    /// Determines if an object should be skipped from tracking.
    /// </summary>
    private static bool ShouldSkipTracking(object obj)
    {
        var type = obj.GetType();
        return type.IsPrimitive || type == typeof(string) || type.IsEnum;
    }
}