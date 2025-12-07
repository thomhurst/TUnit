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

    /// <summary>
    /// Clears all static tracking state. Call at the end of a test session to release memory.
    /// </summary>
    public static void ClearStaticTracking()
    {
        s_trackedObjects.Clear();
    }

    /// <summary>
    /// Flattens a ConcurrentDictionary of depth-keyed HashSets into a single HashSet.
    /// Thread-safe: locks each HashSet while copying.
    /// </summary>
    private static HashSet<object> FlattenTrackedObjects(ConcurrentDictionary<int, HashSet<object>> trackedObjects)
    {
        var result = new HashSet<object>(Helpers.ReferenceEqualityComparer.Instance);
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

    public static void OnDisposed(object? o, Action action)
    {
        if (o is not IDisposable and not IAsyncDisposable)
        {
            return;
        }

        s_trackedObjects.GetOrAdd(o, static _ => new Counter())
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
        if (o is not IDisposable and not IAsyncDisposable)
        {
            return;
        }

        // Avoid async void pattern by wrapping in fire-and-forget with exception handling
        s_trackedObjects.GetOrAdd(o, static _ => new Counter())
            .OnCountChanged += (_, count) =>
        {
            if (count == 0)
            {
                // Fire-and-forget with exception handling to avoid unobserved exceptions
                _ = SafeExecuteAsync(asyncAction);
            }
        };
    }

    /// <summary>
    /// Executes an async action safely, catching and logging any exceptions
    /// to avoid unobserved task exceptions from fire-and-forget patterns.
    /// </summary>
    private static async Task SafeExecuteAsync(Func<Task> asyncAction)
    {
        try
        {
            await asyncAction().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Log to debug in DEBUG builds, otherwise swallow to prevent crashes
            // The disposal itself already logged any errors
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ObjectTracker] Exception in OnDisposedAsync callback: {ex.Message}");
#endif
            // Prevent unobserved task exception from crashing the application
            _ = ex;
        }
    }
}
