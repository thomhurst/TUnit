using System.Collections.Concurrent;
using TUnit.Core.Helpers;

namespace TUnit.Core.Tracking;

/// <summary>
/// Pure reference counting object tracker for disposable objects.
/// Objects are disposed when their reference count reaches zero, regardless of sharing type.
/// </summary>
public static class ObjectTracker
{
    private static readonly ConcurrentDictionary<object, Counter> _trackedObjects = new();
    private static readonly ConcurrentDictionary<object, HashSet<object>> _ownedObjects = new();

    /// <summary>
    /// Tracks multiple objects for a test context and registers a single disposal handler.
    /// Each object's reference count is incremented once.
    /// </summary>
    /// <param name="events">Events for the test instance</param>
    /// <param name="objects">The objects to track (constructor args, method args, injected properties)</param>
    internal static void TrackObjectsForContext(TestContextEvents events, IEnumerable<object?> objects)
    {
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
    internal static void TrackObject(TestContextEvents events, object? obj)
    {
        if (obj == null || ShouldSkipTracking(obj))
        {
            return;
        }

        var counter = _trackedObjects.GetOrAdd(obj, _ => new Counter());

        counter.Increment();

        var objType = obj.GetType().Name;

        events.OnDispose += async (_, _) =>
        {
            var count = counter.Decrement();

            if (count < 0)
            {
                throw new InvalidOperationException($"Reference count for object {objType} went below zero. This indicates a bug in the reference counting logic.");
            }

            if (count == 0)
            {
                await GlobalContext.Current.Disposer.DisposeAsync(obj);
            }
        };
    }

    /// <summary>
    /// Determines if an object should be skipped from tracking.
    /// </summary>
    private static bool ShouldSkipTracking(object? obj)
    {
        return obj is not IDisposable and not IAsyncDisposable;
    }

    public static void OnDisposed(object? o, Action action)
    {
        if(o is not IDisposable and not IAsyncDisposable)
        {
            return;
        }

        _trackedObjects.GetOrAdd(o, _ => new Counter())
            .OnCountChanged += (_, count) =>
        {
            if (count == 0)
            {
                action();
            }
        };
    }
    
    /// <summary>
    /// Tracks that an owner object owns another object.
    /// This increments the owned object's reference count.
    /// When the owner is disposed, it will decrement the owned object's reference count.
    /// </summary>
    public static void TrackOwnership(object owner, object owned)
    {
        if (owner == null || owned == null || ShouldSkipTracking(owned))
        {
            return;
        }
        
        var ownedSet = _ownedObjects.GetOrAdd(owner, _ => new HashSet<object>());
        lock (ownedSet)
        {
            if (ownedSet.Add(owned))
            {
                var counter = _trackedObjects.GetOrAdd(owned, _ => new Counter());
                counter.Increment();
                
                OnDisposed(owner, () =>
                {
                    ReleaseOwnedObjects(owner);
                });
            }
        }
    }
    
    private static void ReleaseOwnedObjects(object owner)
    {
        if (_ownedObjects.TryRemove(owner, out var ownedSet))
        {
            lock (ownedSet)
            {
                foreach (var owned in ownedSet)
                {
                    if (_trackedObjects.TryGetValue(owned, out var counter))
                    {
                        var count = counter.Decrement();
                        if (count == 0)
                        {
                            _ = GlobalContext.Current.Disposer.DisposeAsync(owned);
                        }
                    }
                }
            }
        }
    }
}
