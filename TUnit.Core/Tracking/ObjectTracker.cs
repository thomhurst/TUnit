using System.Collections.Concurrent;
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
    /// Gets an existing counter for the object or creates a new one.
    /// Centralizes the GetOrAdd pattern to ensure consistent counter creation.
    /// </summary>
    private static Counter GetOrCreateCounter(object obj) =>
        s_trackedObjects.GetOrAdd(obj, static _ => new Counter());

    /// <summary>
    /// Counts total tracked objects across all depth levels without allocating a new collection.
    /// </summary>
    private static int CountTrackedObjects(Dictionary<int, HashSet<object>> trackedObjects)
    {
        var count = 0;
        foreach (var kvp in trackedObjects)
        {
            count += kvp.Value.Count;
        }

        return count;
    }

    /// <summary>
    /// Takes a snapshot of currently tracked objects before new discovery mutates the dictionary.
    /// Uses ReferenceEqualityComparer to match object identity semantics.
    /// </summary>
    private static HashSet<object> SnapshotTrackedObjects(Dictionary<int, HashSet<object>> trackedObjects)
    {
        var snapshot = new HashSet<object>(Helpers.ReferenceEqualityComparer.Instance);
        foreach (var kvp in trackedObjects)
        {
            foreach (var obj in kvp.Value)
            {
                snapshot.Add(obj);
            }
        }

        return snapshot;
    }

    public void TrackObjects(TestContext testContext)
    {
        var alreadyTrackedSnapshot = SnapshotTrackedObjects(testContext.TrackedObjects);

        var trackableDict = trackableObjectGraphProvider.GetTrackableObjects(testContext);
        if (trackableDict.Count == 0 && alreadyTrackedSnapshot.Count == 0)
        {
            return;
        }

        // No lock needed: TrackedObjects is per-TestContext and TrackObjects is called
        // from a single thread per test. The previous lock(kvp.Value) caused unnecessary
        // Monitor.Enter_Slowpath contention (~1.25% CPU) during parallel test execution.
        foreach (var kvp in trackableDict)
        {
            foreach (var obj in kvp.Value)
            {
                if (!alreadyTrackedSnapshot.Contains(obj))
                {
                    TrackObject(obj);
                }
            }
        }
    }

    public ValueTask UntrackObjects(TestContext testContext, List<Exception> cleanupExceptions)
    {
        var trackedObjects = testContext.TrackedObjects;

        if (CountTrackedObjects(trackedObjects) == 0)
        {
            return ValueTask.CompletedTask;
        }

        return UntrackObjectsAsync(cleanupExceptions, trackedObjects);
    }

    private async ValueTask UntrackObjectsAsync(List<Exception> cleanupExceptions, Dictionary<int, HashSet<object>> trackedObjects)
    {
        foreach (var depth in trackedObjects.Keys.OrderByDescending(k => k))
        {
            var bucket = trackedObjects[depth];
            List<Task>? disposalTasks = null;

            foreach (var obj in bucket)
            {
                try
                {
                    var task = UntrackObject(obj);

                    if (!task.IsCompletedSuccessfully)
                    {
                        disposalTasks ??= [];
                        disposalTasks.Add(task.AsTask());
                    }
                }
                catch (Exception e)
                {
                    cleanupExceptions.Add(e);
                }
            }

            if (disposalTasks is { Count: > 0 })
            {
                var whenAllTask = Task.WhenAll(disposalTasks);
                try
                {
                    await whenAllTask.ConfigureAwait(false);
                }
                catch
                {
                    foreach (var e in whenAllTask.Exception!.InnerExceptions)
                    {
                        cleanupExceptions.Add(e);
                    }
                }
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

        var counter = GetOrCreateCounter(obj);
        counter.Increment();
    }

    private async ValueTask UntrackObject(object? obj)
    {
        if (obj == null || ShouldSkipTracking(obj))
        {
            return;
        }

        var shouldDispose = false;

        if (s_trackedObjects.TryGetValue(obj, out var counter))
        {
            int count;
            try { count = counter.Decrement(); }
            catch { s_trackedObjects.TryRemove(obj, out _); throw; }

            if (count < 0)
            {
                throw new InvalidOperationException("Reference count for object went below zero. This indicates a bug in the reference counting logic.");
            }

            if (count == 0)
            {
                // Remove from tracking dictionary to prevent memory leak
                // Use TryRemove to ensure atomicity - only remove if still in dictionary
                s_trackedObjects.TryRemove(obj, out _);
                shouldDispose = true;
            }
        }

        // Dispose outside the lock to avoid blocking other untrack operations
        if (shouldDispose)
        {
            await disposer.DisposeAsync(obj).ConfigureAwait(false);
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
    /// If the object is already disposed (or was never tracked), the callback is invoked immediately.
    /// The callback is guaranteed to be invoked exactly once (idempotent).
    /// </summary>
    /// <param name="o">The object to monitor for disposal. If null or not disposable, the method returns without action.</param>
    /// <param name="action">The callback to invoke on disposal. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
    public static void OnDisposed(object? o, Action action)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(action);
#else
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }
#endif

        RegisterDisposalCallback(o, action, static a => a());
    }

    /// <summary>
    /// Registers an async callback to be invoked when the object is disposed (ref count reaches 0).
    /// If the object is already disposed (or was never tracked), the callback is invoked immediately.
    /// The callback is guaranteed to be invoked exactly once (idempotent).
    /// </summary>
    /// <param name="o">The object to monitor for disposal. If null or not disposable, the method returns without action.</param>
    /// <param name="asyncAction">The async callback to invoke on disposal. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="asyncAction"/> is null.</exception>
    public static void OnDisposedAsync(object? o, Func<Task> asyncAction)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(asyncAction);
#else
        if (asyncAction == null)
        {
            throw new ArgumentNullException(nameof(asyncAction));
        }
#endif

        // Wrap async action in fire-and-forget with exception collection
        RegisterDisposalCallback(o, asyncAction, static a => _ = SafeExecuteAsync(a));
    }

    /// <summary>
    /// Core implementation for registering disposal callbacks.
    /// Extracts common logic from OnDisposed and OnDisposedAsync (DRY principle).
    /// </summary>
    /// <typeparam name="TAction">The type of action (Action or Func&lt;Task&gt;).</typeparam>
    /// <param name="o">The object to monitor for disposal.</param>
    /// <param name="action">The callback action.</param>
    /// <param name="invoker">How to invoke the action (sync vs async wrapper).</param>
    private static void RegisterDisposalCallback<TAction>(
        object? o,
        TAction action,
        Action<TAction> invoker)
        where TAction : Delegate
    {
        if (o is not IDisposable and not IAsyncDisposable)
        {
            return;
        }

        // Only register callback if the object is actually being tracked.
        // If not tracked, invoke callback immediately (object is effectively "disposed").
        // This prevents creating spurious counters for untracked objects.
        if (!s_trackedObjects.TryGetValue(o, out var counter))
        {
            // Object not tracked - invoke callback immediately
            invoker(action);
            return;
        }

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

                invoker(action);
            }
        };

        counter.OnCountChanged += handler;

        // Check if already disposed (count is 0) - invoke immediately if so
        // This prevents lost callbacks when registering after disposal
        // Idempotent check ensures this doesn't double-fire if event already triggered
        if (counter.CurrentCount == 0 && Interlocked.Exchange(ref invoked, 1) == 0)
        {
            counter.OnCountChanged -= handler;
            invoker(action);
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
