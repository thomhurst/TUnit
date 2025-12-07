using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;

namespace TUnit.Core.Tracking;

/// <summary>
/// Pure reference counting object tracker for disposable objects.
/// Objects are disposed when their reference count reaches zero, regardless of sharing type.
/// Uses ReferenceEqualityComparer to track objects by identity, not value equality.
/// </summary>
/// <remarks>
/// The static <c>s_trackedObjects</c> dictionary is shared across all tests.
/// Call <see cref="ClearStaticTracking"/> at the end of a test session to release memory.
/// </remarks>
internal class ObjectTracker(TrackableObjectGraphProvider trackableObjectGraphProvider, Disposer disposer)
{
    // Use ReferenceEqualityComparer to prevent objects with custom Equals from sharing state
    private static readonly ConcurrentDictionary<object, Counter> s_trackedObjects =
        new(Helpers.ReferenceEqualityComparer.Instance);

    // Collects errors from async disposal callbacks for post-session review
    private static readonly ConcurrentBag<Exception> s_asyncCallbackErrors = new();

    /// <summary>
    /// Gets any errors that occurred during async disposal callbacks.
    /// Check this at the end of a test session to surface hidden failures.
    /// </summary>
    public static IReadOnlyCollection<Exception> GetAsyncCallbackErrors() => s_asyncCallbackErrors.ToArray();

    /// <summary>
    /// Clears all static tracking state. Call at the end of a test session to release memory.
    /// </summary>
    public static void ClearStaticTracking()
    {
        s_trackedObjects.Clear();
        s_asyncCallbackErrors.Clear();
    }

    /// <summary>
    /// Flattens a ConcurrentDictionary of depth-keyed HashSets into a single HashSet.
    /// Thread-safe: locks each HashSet while copying.
    /// Pre-calculates capacity to avoid HashSet resizing during population.
    /// </summary>
    private static HashSet<object> FlattenTrackedObjects(ConcurrentDictionary<int, HashSet<object>> trackedObjects)
    {
#if NETSTANDARD2_0
        // .NET Standard 2.0 doesn't support HashSet capacity constructor
        var result = new HashSet<object>(Helpers.ReferenceEqualityComparer.Instance);
#else
        // First pass: calculate total capacity to avoid resizing
        var totalCapacity = 0;
        foreach (var kvp in trackedObjects)
        {
            lock (kvp.Value)
            {
                totalCapacity += kvp.Value.Count;
            }
        }

        // Second pass: populate with pre-sized HashSet
        var result = new HashSet<object>(totalCapacity, Helpers.ReferenceEqualityComparer.Instance);
#endif
        foreach (var kvp in trackedObjects)
        {
            lock (kvp.Value)
            {
                foreach (var obj in kvp.Value)
                {
                    result.Add(obj);
                }
            }
        }

        return result;
    }

    public void TrackObjects(TestContext testContext)
    {
        // Get already tracked objects (DRY: use helper method)
        var alreadyTracked = FlattenTrackedObjects(testContext.TrackedObjects);

        // Get new trackable objects
        var newTrackableObjects = new HashSet<object>(Helpers.ReferenceEqualityComparer.Instance);
        var trackableDict = trackableObjectGraphProvider.GetTrackableObjects(testContext);
        foreach (var kvp in trackableDict)
        {
            lock (kvp.Value)
            {
                foreach (var obj in kvp.Value)
                {
                    if (!alreadyTracked.Contains(obj))
                    {
                        newTrackableObjects.Add(obj);
                    }
                }
            }
        }

        foreach (var obj in newTrackableObjects)
        {
            TrackObject(obj);
        }
    }

    public async ValueTask UntrackObjects(TestContext testContext, List<Exception> cleanupExceptions)
    {
        // Get all objects to untrack (DRY: use helper method)
        var objectsToUntrack = FlattenTrackedObjects(testContext.TrackedObjects);

        foreach (var obj in objectsToUntrack)
        {
            try
            {
                await UntrackObject(obj);
            }
            catch (Exception e)
            {
                cleanupExceptions.Add(e);
            }
        }
    }

    /// <summary>
    /// Track static property objects (session-level)
    /// </summary>
    public void TrackStaticProperties()
    {
        // Incrementing them by 1 if they're static ensures they don't get disposed until the end of the session
        var objects = trackableObjectGraphProvider.GetStaticPropertyTrackableObjects();

        foreach (var obj in objects)
        {
            TrackObject(obj);
        }
    }

    /// <summary>
    /// Tracks a single object for a test context.
    /// For backward compatibility - adds to existing tracked objects for the context.
    /// </summary>
    /// <param name="obj">The object to track</param>
    private void TrackObject(object? obj)
    {
        if (obj == null || ShouldSkipTracking(obj))
        {
            return;
        }

        var counter = s_trackedObjects.GetOrAdd(obj, static _ => new Counter());
        counter.Increment();
    }

    private async ValueTask UntrackObject(object? obj)
    {
        if (obj == null || ShouldSkipTracking(obj))
        {
            return;
        }

        if (s_trackedObjects.TryGetValue(obj, out var counter))
        {
            var count = counter.Decrement();

            if (count < 0)
            {
                throw new InvalidOperationException("Reference count for object went below zero. This indicates a bug in the reference counting logic.");
            }

            if (count == 0)
            {
                // Remove from tracking dictionary to prevent memory leak
                // Use TryRemove to ensure atomicity - only remove if still in dictionary
                s_trackedObjects.TryRemove(obj, out _);

                await disposer.DisposeAsync(obj).ConfigureAwait(false);
            }
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
    /// Registers a callback to be invoked when the object is disposed (ref count reaches 0).
    /// If the object is already disposed, the callback is invoked immediately.
    /// The callback is guaranteed to be invoked exactly once (idempotent).
    /// </summary>
    public static void OnDisposed(object? o, Action action)
    {
        if (o is not IDisposable and not IAsyncDisposable)
        {
            return;
        }

        var counter = s_trackedObjects.GetOrAdd(o, static _ => new Counter());

        // Use flag to ensure callback only fires once (idempotent)
        var invoked = 0;
        EventHandler<int>? handler = null;

        handler = (sender, count) =>
        {
            if (count == 0 && Interlocked.Exchange(ref invoked, 1) == 0)
            {
                // Remove handler to prevent memory leaks
                if (sender is Counter c && handler != null)
                {
                    c.OnCountChanged -= handler;
                }

                action();
            }
        };

        counter.OnCountChanged += handler;

        // Check if already disposed (count is 0) - invoke immediately if so
        // This prevents lost callbacks when registering after disposal
        // Idempotent check ensures this doesn't double-fire if event already triggered
        if (counter.CurrentCount == 0 && Interlocked.Exchange(ref invoked, 1) == 0)
        {
            counter.OnCountChanged -= handler;
            action();
        }
    }

    /// <summary>
    /// Registers an async callback to be invoked when the object is disposed (ref count reaches 0).
    /// If the object is already disposed, the callback is invoked immediately.
    /// The callback is guaranteed to be invoked exactly once (idempotent).
    /// </summary>
    public static void OnDisposedAsync(object? o, Func<Task> asyncAction)
    {
        if (o is not IDisposable and not IAsyncDisposable)
        {
            return;
        }

        var counter = s_trackedObjects.GetOrAdd(o, static _ => new Counter());

        // Use flag to ensure callback only fires once (idempotent)
        var invoked = 0;
        EventHandler<int>? handler = null;

        // Avoid async void pattern by wrapping in fire-and-forget with exception handling
        handler = (sender, count) =>
        {
            if (count == 0 && Interlocked.Exchange(ref invoked, 1) == 0)
            {
                // Remove handler to prevent memory leaks
                if (sender is Counter c && handler != null)
                {
                    c.OnCountChanged -= handler;
                }

                // Fire-and-forget with exception collection to surface errors
                _ = SafeExecuteAsync(asyncAction);
            }
        };

        counter.OnCountChanged += handler;

        // Check if already disposed (count is 0) - invoke immediately if so
        // This prevents lost callbacks when registering after disposal
        // Idempotent check ensures this doesn't double-fire if event already triggered
        if (counter.CurrentCount == 0 && Interlocked.Exchange(ref invoked, 1) == 0)
        {
            counter.OnCountChanged -= handler;
            _ = SafeExecuteAsync(asyncAction);
        }
    }

    /// <summary>
    /// Executes an async action safely, catching and collecting exceptions
    /// for post-session review instead of silently swallowing them.
    /// </summary>
    private static async Task SafeExecuteAsync(Func<Task> asyncAction)
    {
        try
        {
            await asyncAction().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Collect error for post-session review instead of silently swallowing
            s_asyncCallbackErrors.Add(ex);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ObjectTracker] Exception in OnDisposedAsync callback: {ex.Message}");
#endif
        }
    }
}
