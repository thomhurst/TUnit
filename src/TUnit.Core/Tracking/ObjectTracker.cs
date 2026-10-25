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
/// Call <see cref="DisposeAndClearStaticTrackingAsync"/> at the end of a run session to
/// dispose any leftovers and release memory.
/// </remarks>
internal class ObjectTracker(TrackableObjectGraphProvider trackableObjectGraphProvider, Disposer disposer)
{
    // Use ReferenceEqualityComparer to prevent objects with custom Equals from sharing state
    private static readonly ConcurrentDictionary<object, Counter> s_trackedObjects =
        new(Helpers.ReferenceEqualityComparer.Instance);

    // Serializes retiring a cached shared instance (count reached zero) with a running test taking
    // a reference to it, so the instance is either retired or held, never both.
    private static readonly object s_sharedInstanceLock = new();

    // Collects errors from async disposal callbacks for post-session review
    private static readonly ConcurrentBag<Exception> s_asyncCallbackErrors = new();

    /// <summary>
    /// Gets any errors that occurred during async disposal callbacks.
    /// Check this at the end of a test session to surface hidden failures.
    /// </summary>
    public static IReadOnlyCollection<Exception> GetAsyncCallbackErrors() => s_asyncCallbackErrors.ToArray();

    /// <summary>
    /// Disposes any objects still tracked with a positive reference count, then clears all
    /// static tracking state. Call at the end of a run session: every executed test has
    /// already decremented its references by then, so anything still alive would otherwise
    /// leak (e.g. its remaining consumers were cancelled or a path miscounted). Clearing also
    /// ensures a subsequent run request in the same process starts with fresh state.
    /// Intentionally an instance method despite operating on static state: disposal needs the
    /// injected <see cref="Disposer"/> (for error logging), which only instances carry.
    /// </summary>
    /// <returns>Any exceptions thrown by the disposals, or null if none.</returns>
    public async ValueTask<List<Exception>?> DisposeAndClearStaticTrackingAsync()
    {
        List<Exception>? exceptions = null;

        foreach (var kvp in s_trackedObjects)
        {
            // TryRemove guards against racing disposals (e.g. a late UntrackObject call)
            if (!s_trackedObjects.TryRemove(kvp.Key, out _))
            {
                continue;
            }

            try
            {
                await disposer.DisposeAsync(kvp.Key).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                (exceptions ??= []).Add(ex);
            }
        }

        s_asyncCallbackErrors.Clear();

        return exceptions;
    }

    /// <summary>
    /// Gets an existing counter for the object or creates a new one.
    /// Centralizes the GetOrAdd pattern to ensure consistent counter creation.
    /// </summary>
    private static Counter GetOrCreateCounter(object obj) =>
        s_trackedObjects.GetOrAdd(obj, static _ => new Counter());

    internal static bool IsShared(object obj) =>
        s_trackedObjects.TryGetValue(obj, out var counter) && counter.CurrentCount > 1;

    /// <summary>
    /// Counts total tracked objects across all depth levels without allocating a new collection.
    /// </summary>
    private static int CountTrackedObjects(SortedList<int, HashSet<object>>? trackedObjects)
    {
        if (trackedObjects is null || trackedObjects.Count == 0)
        {
            return 0;
        }

        // Indexed access: SortedList's enumerator is a class, so foreach would allocate.
        var values = trackedObjects.Values;
        var count = 0;
        for (var i = 0; i < values.Count; i++)
        {
            count += values[i].Count;
        }

        return count;
    }

    /// <summary>
    /// Takes a snapshot of currently tracked objects before new discovery mutates the dictionary.
    /// Uses ReferenceEqualityComparer to match object identity semantics.
    /// </summary>
    private static HashSet<object>? SnapshotTrackedObjects(SortedList<int, HashSet<object>>? trackedObjects)
    {
        if (CountTrackedObjects(trackedObjects) == 0)
        {
            return null;
        }

        var snapshot = new HashSet<object>(Helpers.ReferenceEqualityComparer.Instance);
        foreach (var kvp in trackedObjects!)
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
        var alreadyTrackedSnapshot = SnapshotTrackedObjects(testContext.TrackedObjectsIfCreated);

        SortedList<int, HashSet<object>> trackableDict;
        try
        {
            trackableDict = trackableObjectGraphProvider.GetTrackableObjects(testContext);
        }
        catch
        {
            // Discovery adds objects to the test's tracked set as it goes, but references are only
            // counted below, once it has finished. Drop what this call added, so that completing the
            // test does not release references it never took (which could dispose a shared instance
            // that another test still holds).
            RemoveObjectsNotIn(testContext.TrackedObjectsIfCreated, alreadyTrackedSnapshot);
            throw;
        }
        if (trackableDict.Count == 0 && alreadyTrackedSnapshot is null)
        {
            return;
        }

        // No lock needed: TrackedObjects is per-TestContext and TrackObjects
        // is called from a single thread per test.
        foreach (var kvp in trackableDict)
        {
            foreach (var obj in kvp.Value)
            {
                if (alreadyTrackedSnapshot?.Contains(obj) != true)
                {
                    TrackObject(obj);
                }
            }
        }
    }

    private static void RemoveObjectsNotIn(SortedList<int, HashSet<object>>? trackedObjects, HashSet<object>? keep)
    {
        if (trackedObjects is null)
        {
            return;
        }

        for (var i = trackedObjects.Count - 1; i >= 0; i--)
        {
            var bucket = trackedObjects.Values[i];
            if (keep is null)
            {
                bucket.Clear();
            }
            else
            {
                bucket.RemoveWhere(obj => !keep.Contains(obj));
            }

            if (bucket.Count == 0)
            {
                trackedObjects.RemoveAt(i);
            }
        }
    }

    public ValueTask<List<Exception>?> UntrackObjects(TestContext testContext)
    {
        var trackedObjects = testContext.TrackedObjectsIfCreated;
        var heldSharedInstances = testContext.HeldSharedInstancesIfCreated;

        if (CountTrackedObjects(trackedObjects) == 0 && heldSharedInstances is not { Count: > 0 })
        {
            return new ValueTask<List<Exception>?>((List<Exception>?)null);
        }

        return UntrackObjectsAsync(trackedObjects, heldSharedInstances);
    }

    private async ValueTask<List<Exception>?> UntrackObjectsAsync(SortedList<int, HashSet<object>>? trackedObjects, HashSet<object>? heldSharedInstances)
    {
        // SortedList keeps keys in ascending order; iterate in ascending order (shallowest depth first).
        // This ensures disposal happens in reverse order of initialization (which goes deepest first).
        // Dependents (shallow) are disposed before their dependencies (deep).
        List<Exception>? cleanupExceptions = null;

        if (trackedObjects is not null)
        {
            var values = trackedObjects.Values;

            for (var i = 0; i < values.Count; i++)
            {
                cleanupExceptions = await UntrackBucketAsync(values[i], cleanupExceptions).ConfigureAwait(false);
            }
        }

        // Released last: the test's own objects may depend on the shared instances it fetched.
        if (heldSharedInstances is { Count: > 0 })
        {
            cleanupExceptions = await UntrackBucketAsync(heldSharedInstances, cleanupExceptions).ConfigureAwait(false);
        }

        return cleanupExceptions;
    }

    private async ValueTask<List<Exception>?> UntrackBucketAsync(HashSet<object> bucket, List<Exception>? cleanupExceptions)
    {
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
                (cleanupExceptions ??= []).Add(e);
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
                (cleanupExceptions ??= []).AddRange(whenAllTask.Exception!.InnerExceptions);
            }
        }

        return cleanupExceptions;
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

    /// <summary>
    /// Decrements a single object's reference count, disposing it (and removing it from the
    /// static tracking dictionary) when the count reaches zero. Used by owners that hold their
    /// own +1 reference (e.g. static properties) so disposal stays consistent with ref counting
    /// instead of bypassing it — a direct dispose would leave a stale entry that the session-end
    /// sweep would dispose a second time.
    /// </summary>
    public ValueTask UntrackObjectAsync(object? obj) => UntrackObject(obj);

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
                shouldDispose = TestDataContainer.IsCached(obj)
                    ? TryRetireSharedInstance(obj, counter)
                    : RemoveCounter(obj, counter);
            }
        }

        if (shouldDispose)
        {
            // TODO: Add disposal span for shared objects here to match the init spans
            // created in ObjectLifecycleService.InitializeWithSpanAsync. Currently only
            // per-test disposal gets a span (via TestCoordinator.DisposeTestInstanceWithSpanAsync).
            await disposer.DisposeAsync(obj).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Removes <paramref name="counter"/> from the tracking dictionary (preventing a memory leak),
    /// but only while it is still the object's counter. Returns whether it was removed. The caller
    /// disposes the object only then, so two callers that both saw the count reach zero (a shared
    /// instance held again by a running test in between) cannot both dispose it.
    /// </summary>
    private static bool RemoveCounter(object obj, Counter counter) =>
        ((ICollection<KeyValuePair<object, Counter>>) s_trackedObjects).Remove(new KeyValuePair<object, Counter>(obj, counter));

    /// <summary>
    /// Stops tracking a cached shared instance whose count has reached zero and evicts it from
    /// <see cref="TestDataContainer"/>, so later requests create a new instance instead of
    /// receiving this one after it is disposed (#7204). Returns false when a running test took a
    /// reference (<see cref="TryHoldForRunningTest"/>) after the count reached zero, or when that
    /// test has already retired it.
    /// </summary>
    private static bool TryRetireSharedInstance(object obj, Counter counter)
    {
        lock (s_sharedInstanceLock)
        {
            // Fails if a running test took a reference after the count reached zero, or if that
            // test has since released it and retired the instance itself.
            if (counter.CurrentCount != 0 || !RemoveCounter(obj, counter))
            {
                return false;
            }

            TestDataContainer.Evict(obj);
            return true;
        }
    }

    /// <summary>
    /// Makes a running test hold a reference to a shared instance that it fetched itself (through
    /// <see cref="SharedDataSources"/>) rather than receiving it from a data source. The reference
    /// is released, and the instance disposed if no other test holds it, when the test's tracked
    /// objects are untracked at the end of the test.
    /// </summary>
    /// <returns>
    /// False only when the instance has already been retired (disposed or about to be), in which
    /// case the caller must fetch the instance again; the cache then creates a new one.
    /// True otherwise, including when no test is running or the test already holds the instance.
    /// </returns>
    internal static bool TryHoldForRunningTest(TestContext testContext, object instance)
    {
        if (ShouldSkipTracking(instance) || !testContext.HoldsSharedReferences)
        {
            return true;
        }

        var heldSharedInstances = testContext.HeldSharedInstances;

        // Code inside a test can fetch shared instances concurrently; the lock also lets
        // TestContext.StopHoldingSharedReferences wait for any add in progress.
        lock (heldSharedInstances)
        {
            // The tracked objects no longer change once the test runs, so they can be read here.
            if (!testContext.HoldsSharedReferences
                || heldSharedInstances.Contains(instance)
                || Contains(testContext.TrackedObjectsIfCreated, instance))
            {
                return true;
            }

            lock (s_sharedInstanceLock)
            {
                if (!TestDataContainer.IsCached(instance))
                {
                    return false;
                }

                GetOrCreateCounter(instance).Increment();
            }

            // Kept out of the tracked objects, so the engine does not initialize it.
            heldSharedInstances.Add(instance);
            return true;
        }
    }

    private static bool Contains(SortedList<int, HashSet<object>>? trackedObjects, object obj)
    {
        if (trackedObjects is null)
        {
            return false;
        }

        var values = trackedObjects.Values;
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i].Contains(obj))
            {
                return true;
            }
        }

        return false;
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
#if NET8_0_OR_GREATER
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
#if NET8_0_OR_GREATER
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
