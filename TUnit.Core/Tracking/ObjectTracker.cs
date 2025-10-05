using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;

namespace TUnit.Core.Tracking;

/// <summary>
/// Pure reference counting object tracker for disposable objects.
/// Objects are disposed when their reference count reaches zero, regardless of sharing type.
/// </summary>
internal class ObjectTracker(TrackableObjectGraphProvider trackableObjectGraphProvider, Disposer disposer)
{
    private static readonly ConcurrentDictionary<object, Counter> _trackedObjects = new();

    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Trackable object discovery uses reflection for property injection")]
    #endif
    public void TrackObjects(TestContext testContext)
    {
        var alreadyTracked = testContext.TrackedObjects.SelectMany(x => x.Value).ToHashSet();

        var newTrackableObjects = trackableObjectGraphProvider.GetTrackableObjects(testContext)
            .SelectMany(x => x.Value)
            .Except(alreadyTracked)
            .ToHashSet();

        foreach (var obj in newTrackableObjects)
        {
            TrackObject(obj);
        }
    }

    public async ValueTask UntrackObjects(TestContext testContext, List<Exception> cleanupExceptions)
    {
        foreach (var obj in testContext.TrackedObjects
                     .SelectMany(x => x.Value)
                     .ToHashSet())
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

        var counter = _trackedObjects.GetOrAdd(obj, _ => new Counter());
        counter.Increment();
    }

    private async ValueTask UntrackObject(object? obj)
    {
        if (obj == null || ShouldSkipTracking(obj))
        {
            return;
        }

        if (_trackedObjects.TryGetValue(obj, out var counter))
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
}
