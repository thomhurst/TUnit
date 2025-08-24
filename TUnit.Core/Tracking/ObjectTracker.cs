using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
    private static readonly ConcurrentDictionary<object, bool> _disposalCallbackRegistered = new();
    private static readonly ConcurrentDictionary<object, bool> _disposalInProgress = new();
    
    // Track which objects have already been tracked for each test context to prevent double-tracking
    private static readonly ConcurrentDictionary<TestContextEvents, HashSet<object>> _contextTrackedObjects = new();

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
    public static void TrackObject(TestContextEvents events, object? obj)
    {
        if (obj == null || ShouldSkipTracking(obj))
        {
            return;
        }

        // Get or create the set of tracked objects for this test context
        var contextTrackedSet = _contextTrackedObjects.GetOrAdd(events, _ => new HashSet<object>());
        
        // Check if we've already tracked this object for this test context
        // This prevents double-tracking within the same test (e.g., as constructor arg AND as property)
        bool alreadyTracked;
        lock (contextTrackedSet)
        {
            alreadyTracked = !contextTrackedSet.Add(obj);
        }
        
        if (alreadyTracked)
        {
            Console.WriteLine($"[ObjectTracker] Skipping duplicate tracking for {obj.GetType().Name} (hash: {obj.GetHashCode()}) in same test context");
            return;
        }

        var counter = _trackedObjects.GetOrAdd(obj, _ => new Counter());

        var newCount = counter.Increment();
        
        // Console output for debugging
        Console.WriteLine($"[ObjectTracker] Incremented count for {obj.GetType().Name} (hash: {obj.GetHashCode()}) to {newCount}");

        // Register a single disposal handler for this object in this context
        bool handlerRegistered = false;
        events.OnDispose += async (object sender, TestContext context) =>
        {
            // Ensure we only decrement once per disposal event
            if (handlerRegistered)
            {
                return;
            }
            handlerRegistered = true;
            
            var count = counter.Decrement();
            
            // Console output for debugging
            Console.WriteLine($"[ObjectTracker] Decremented count for {obj.GetType().Name} (hash: {obj.GetHashCode()}) to {count}");

            if (count < 0)
            {
                throw new InvalidOperationException($"Reference count for object went below zero. This indicates a bug in the reference counting logic.");
            }

            if (count == 0)
            {
                Console.WriteLine($"[ObjectTracker] Disposing {obj.GetType().Name} (hash: {obj.GetHashCode()})");
                await GlobalContext.Current.Disposer.DisposeAsync(obj).ConfigureAwait(false);
                _trackedObjects.TryRemove(obj, out _);
                Console.WriteLine($"[ObjectTracker] Disposed {obj.GetType().Name} (hash: {obj.GetHashCode()})");
            }
        };
        
        // Clean up the context tracking when the test is done (do this separately to ensure it happens)
        Func<object, TestContext, ValueTask> cleanupHandler = (object sender, TestContext context) =>
        {
            _contextTrackedObjects.TryRemove(events, out _);
            return default(ValueTask);
        };
        events.OnDispose += cleanupHandler;
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
    
    public static void OnDisposedAsync(object? o, Func<Task> asyncAction)
    {
        if(o is not IDisposable and not IAsyncDisposable)
        {
            return;
        }

        _trackedObjects.GetOrAdd(o, _ => new Counter())
            .OnCountChanged += async (_, count) =>
        {
            if (count == 0)
            {
                await asyncAction().ConfigureAwait(false);
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
        
        // Prevent circular ownership - owner cannot own itself
        if (ReferenceEquals(owner, owned))
        {
            return;
        }
        
        // Check if this would create a circular ownership chain
        if (IsOwnedBy(owned, owner))
        {
            // owned already owns owner, so we can't make owner own owned
            return;
        }
        
        var ownedSet = _ownedObjects.GetOrAdd(owner, _ => new HashSet<object>());
        bool shouldRegisterCallback = false;
        
        lock (ownedSet)
        {
            if (ownedSet.Add(owned))
            {
                var counter = _trackedObjects.GetOrAdd(owned, _ => new Counter());
                counter.Increment();
                
                // Check if we need to register disposal callback (but do it outside the lock)
                if (!_disposalCallbackRegistered.ContainsKey(owner))
                {
                    shouldRegisterCallback = _disposalCallbackRegistered.TryAdd(owner, true);
                }
            }
        }
        
        // Register disposal callback OUTSIDE the lock to prevent deadlocks
        if (shouldRegisterCallback)
        {
            OnDisposedAsync(owner, async () =>
            {
                await ReleaseOwnedObjectsAsync(owner).ConfigureAwait(false);
            });
        }
    }
    
    private static bool IsOwnedBy(object potentialOwner, object potentialOwned)
    {
        // Check if potentialOwner owns potentialOwned (directly or indirectly)
        var visited = new HashSet<object>();
        var stack = new Stack<object>();
        stack.Push(potentialOwner);
        
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current))
            {
                continue; // Already visited
            }
            
            if (ReferenceEquals(current, potentialOwned))
            {
                return true; // Found circular ownership
            }
            
            if (_ownedObjects.TryGetValue(current, out var ownedSet))
            {
                lock (ownedSet)
                {
                    foreach (var owned in ownedSet)
                    {
                        stack.Push(owned);
                    }
                }
            }
        }
        
        return false;
    }
    
    private static async Task ReleaseOwnedObjectsAsync(object owner)
    {
        // Prevent multiple concurrent disposal attempts for the same owner
        if (!_disposalInProgress.TryAdd(owner, true))
        {
            return; // Already being disposed
        }
        
        try
        {
            if (_ownedObjects.TryRemove(owner, out var ownedSet))
            {
                List<Task> disposalTasks = new();
                List<object> objectsToDispose = new();
                
                // CRITICAL FIX: Collect objects to dispose OUTSIDE the lock to prevent deadlocks
                lock (ownedSet)
                {
                    foreach (var owned in ownedSet)
                    {
                        if (_trackedObjects.TryGetValue(owned, out var counter))
                        {
                            var count = counter.Decrement();
                            if (count == 0)
                            {
                                // Mark as being disposed to prevent multiple disposal attempts
                                if (_disposalInProgress.TryAdd(owned, true))
                                {
                                    objectsToDispose.Add(owned);
                                }
                            }
                        }
                    }
                }
                
                // Now create disposal tasks OUTSIDE the lock
                foreach (var obj in objectsToDispose)
                {
                    disposalTasks.Add(GlobalContext.Current.Disposer.DisposeAsync(obj).AsTask());
                }
                
                // Dispose all owned objects in parallel with timeout protection
                if (disposalTasks.Count > 0)
                {
                    using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
                    try
                    {
                        await Task.WhenAll(disposalTasks).WaitAsync(cts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Disposal timed out - log and continue
                        System.Diagnostics.Debug.WriteLine($"Disposal of objects owned by {owner.GetType().Name} timed out after 30 seconds");
                    }
                }
            }
        }
        finally
        {
            // Clean up tracking dictionaries for disposed objects
            _disposalCallbackRegistered.TryRemove(owner, out _);
            _disposalInProgress.TryRemove(owner, out _);
        }
    }
}
